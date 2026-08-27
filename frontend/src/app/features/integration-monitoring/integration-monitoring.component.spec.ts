import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { IntegrationMonitoringComponent } from './integration-monitoring.component';
import { IntegrationMonitoringApiService, IntegrationLogDto } from './services/integration-monitoring-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { ToastService } from '@/shared/services/toast/toast.service';

describe('IntegrationMonitoringComponent', () => {
  let component: IntegrationMonitoringComponent;
  let fixture: ComponentFixture<IntegrationMonitoringComponent>;
  let apiService: IntegrationMonitoringApiService;
  let currentUserService: CurrentUserService;
  let toastService: ToastService;

  const mockFailedLog: IntegrationLogDto = {
    id: 1,
    targetSystem: 'Databank',
    analysisId: 501,
    status: 'Failed',
    attemptedAtUtc: '2026-08-20T10:00:00Z',
    completedAtUtc: null,
    errorMessage: 'Timeout contacting Databank',
    retryCount: 2,
  };

  const mockSuccessLog: IntegrationLogDto = {
    id: 2,
    targetSystem: 'SCADA',
    analysisId: 502,
    status: 'Success',
    attemptedAtUtc: '2026-08-20T09:00:00Z',
    completedAtUtc: '2026-08-20T09:01:00Z',
    errorMessage: null,
    retryCount: 0,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IntegrationMonitoringComponent],
      providers: [IntegrationMonitoringApiService, CurrentUserService, provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(IntegrationMonitoringComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(IntegrationMonitoringApiService);
    currentUserService = TestBed.inject(CurrentUserService);
    toastService = TestBed.inject(ToastService);

    const mockUser = { sub: 'user1', username: 'coordinator', role: 'LabCoordinator' as const, site: 'Site1' };
    vi.spyOn(currentUserService, 'user').mockReturnValue(mockUser);
  });

  describe('loading', () => {
    it('shows loading while fetching', () => {
      vi.spyOn(apiService, 'listLogs').mockReturnValue(of([]));
      component.loading.set(true);
      expect(component.loading()).toBe(true);
    });

    it('loads logs on init', () => {
      vi.spyOn(apiService, 'listLogs').mockReturnValue(of([mockFailedLog]));

      component.ngOnInit();

      expect(component.logs()).toEqual([mockFailedLog]);
      expect(component.loading()).toBe(false);
    });
  });

  describe('forbidden', () => {
    it('sets forbidden on 403', () => {
      vi.spyOn(apiService, 'listLogs').mockReturnValue(throwError(() => ({ status: 403 })));

      component.ngOnInit();

      expect(component.forbidden()).toBe(true);
      expect(component.loading()).toBe(false);
    });
  });

  describe('error', () => {
    it('sets error message on non-403 failure', () => {
      vi.spyOn(apiService, 'listLogs').mockReturnValue(throwError(() => ({ status: 500 })));

      component.ngOnInit();

      expect(component.error()).toContain('Failed to load');
      expect(component.forbidden()).toBe(false);
    });
  });

  describe('empty', () => {
    it('has no logs when API returns empty array', () => {
      vi.spyOn(apiService, 'listLogs').mockReturnValue(of([]));

      component.ngOnInit();

      expect(component.logs()).toEqual([]);
    });
  });

  describe('status filter', () => {
    it('refetches with the selected status', () => {
      const spy = vi.spyOn(apiService, 'listLogs').mockReturnValue(of([mockFailedLog]));

      component.onStatusFilterChange('Failed');

      expect(spy).toHaveBeenCalledWith(expect.objectContaining({ status: 'Failed' }));
    });

    it('omits status filter when All is selected', () => {
      const spy = vi.spyOn(apiService, 'listLogs').mockReturnValue(of([]));

      component.onStatusFilterChange('All');

      expect(spy).toHaveBeenCalledWith(expect.objectContaining({ status: undefined }));
    });

    it('refetches with the selected target system', () => {
      const spy = vi.spyOn(apiService, 'listLogs').mockReturnValue(of([]));

      component.onTargetSystemFilterChange('SCADA');

      expect(spy).toHaveBeenCalledWith(expect.objectContaining({ targetSystem: 'SCADA' }));
    });
  });

  describe('counts', () => {
    it('computes failed/pending/success counts', () => {
      vi.spyOn(apiService, 'listLogs').mockReturnValue(of([mockFailedLog, mockSuccessLog]));

      component.ngOnInit();

      expect(component.failedCount()).toBe(1);
      expect(component.successCount()).toBe(1);
      expect(component.pendingCount()).toBe(0);
    });
  });

  describe('reprocess', () => {
    beforeEach(() => {
      vi.spyOn(apiService, 'listLogs').mockReturnValue(of([mockFailedLog]));
      component.ngOnInit();
    });

    it('reprocesses and reloads on success', () => {
      vi.spyOn(apiService, 'reprocess').mockReturnValue(
        of({ id: 1, status: 'Success', success: true, message: 'ok' })
      );
      const reloadSpy = vi.spyOn(apiService, 'listLogs');
      const toastSpy = vi.spyOn(toastService, 'success');

      component.reprocess(mockFailedLog);

      expect(apiService.reprocess).toHaveBeenCalledWith(1);
      expect(reloadSpy).toHaveBeenCalledTimes(2); // initial load + reload after success
      expect(toastSpy).toHaveBeenCalledWith('Reprocess attempted for #501.');
      expect(component.isReprocessing(1)).toBe(false);
    });

    it('sets in-flight state while the call is pending', () => {
      vi.spyOn(apiService, 'reprocess').mockReturnValue(of({ id: 1, status: 'Failed', success: false, message: '' }));

      expect(component.isReprocessing(1)).toBe(false);
      component.reprocess(mockFailedLog);
      // Synchronous observable resolves immediately in this test, so it clears right away.
      expect(component.isReprocessing(1)).toBe(false);
    });

    it('shows the 400 detail message when reprocessing is unsupported', () => {
      vi.spyOn(apiService, 'reprocess').mockReturnValue(
        throwError(() => ({ status: 400, error: { detail: 'DataLakehouse does not support reprocessing.' } }))
      );
      const toastSpy = vi.spyOn(toastService, 'error');

      component.reprocess(mockFailedLog);

      expect(toastSpy).toHaveBeenCalledWith('DataLakehouse does not support reprocessing.');
      expect(component.isReprocessing(1)).toBe(false);
    });

    it('shows a generic error toast for non-400 failures', () => {
      vi.spyOn(apiService, 'reprocess').mockReturnValue(throwError(() => ({ status: 500 })));
      const toastSpy = vi.spyOn(toastService, 'error');

      component.reprocess(mockFailedLog);

      expect(toastSpy).toHaveBeenCalledWith('Failed to reprocess. Please try again.');
      expect(component.isReprocessing(1)).toBe(false);
    });

    it('does not start a second call while one is in flight', () => {
      component.reprocessingIds.set(new Set([1]));
      const spy = vi.spyOn(apiService, 'reprocess');

      component.reprocess(mockFailedLog);

      expect(spy).not.toHaveBeenCalled();
    });
  });

  describe('permission gating', () => {
    it('returns true for Lab Coordinator', () => {
      expect(component.isLabCoordinator()).toBe(true);
    });

    it('returns false for non-Lab Coordinator', () => {
      vi.spyOn(currentUserService, 'user').mockReturnValue({
        sub: 'user2',
        username: 'analyst',
        role: 'ControlLabAnalyst',
        site: 'Site1',
      });

      expect(component.isLabCoordinator()).toBe(false);
    });
  });
});

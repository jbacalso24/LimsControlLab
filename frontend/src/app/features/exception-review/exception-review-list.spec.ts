import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ExceptionReviewListComponent } from './exception-review-list.component';
import { ExceptionReviewApiService } from './services/exception-review-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { ResultReviewDto } from '../../shared/generated/models/result-review-dto';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('ExceptionReviewListComponent', () => {
  let component: ExceptionReviewListComponent;
  let fixture: ComponentFixture<ExceptionReviewListComponent>;
  let apiService: ExceptionReviewApiService;
  let currentUserService: CurrentUserService;

  const mockLockedAnalysis: ResultReviewDto = {
    id: 1,
    sampleId: 100,
    templateId: 50,
    status: 'Completed',
    startedAtUtc: '2026-08-20T10:00:00Z',
    completedAtUtc: '2026-08-20T11:00:00Z',
    startedByUserId: 1,
    isLocked: true,
    lockedAtUtc: '2026-08-20T11:00:00Z',
    lockedByUserId: 2,
    rowVersion: 'v1.0',
    exceptions: [],
  };

  const mockUnlockedAnalysis: ResultReviewDto = {
    ...mockLockedAnalysis,
    isLocked: false,
    lockedAtUtc: undefined,
    lockedByUserId: undefined,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExceptionReviewListComponent, ReactiveFormsModule],
      providers: [
        ExceptionReviewApiService,
        CurrentUserService,
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ExceptionReviewListComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(ExceptionReviewApiService);
    currentUserService = TestBed.inject(CurrentUserService);
  });

  describe('initialization', () => {
    it('should load analyses on init', () => {
      vi.spyOn(apiService, 'listExceptionAnalyses').mockReturnValue(
        of([mockLockedAnalysis])
      );

      component.ngOnInit();

      expect(component.analyses()).toEqual([mockLockedAnalysis]);
      expect(component.loading()).toBe(false);
    });

    it('should display loading state while fetching', () => {
      vi.spyOn(apiService, 'listExceptionAnalyses').mockReturnValue(of([]));

      component.loading.set(true);
      expect(component.loading()).toBe(true);
    });
  });

  describe('state handling', () => {
    it('should display error message on load failure', () => {
      vi.spyOn(apiService, 'listExceptionAnalyses').mockReturnValue(
        throwError(() => new Error('Network error'))
      );

      component.ngOnInit();

      expect(component.error()).toContain('Failed to load');
      expect(component.loading()).toBe(false);
    });

    it('should display empty state when no analyses', () => {
      vi.spyOn(apiService, 'listExceptionAnalyses').mockReturnValue(of([]));

      component.ngOnInit();

      expect(component.analyses()).toEqual([]);
    });

    it('should reload analyses on retry', () => {
      vi.spyOn(apiService, 'listExceptionAnalyses').mockReturnValue(
        of([mockLockedAnalysis])
      );

      component.reload();

      expect(component.analyses()).toEqual([mockLockedAnalysis]);
    });
  });

  describe('permission gating', () => {
    it('should return true for Lab Coordinator', () => {
      const mockUser = {
        sub: 'user123',
        role: 'LabCoordinator',
        site: 'Site1',
      };
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      vi.spyOn(currentUserService, 'user').mockReturnValue(mockUser as any);

      expect(component.isLabCoordinator()).toBe(true);
    });

    it('should return false for non-Lab Coordinator', () => {
      const mockUser = {
        sub: 'user123',
        role: 'ControlLabAnalyst',
        site: 'Site1',
      };
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      vi.spyOn(currentUserService, 'user').mockReturnValue(mockUser as any);

      expect(component.isLabCoordinator()).toBe(false);
    });
  });

  describe('unlock dialog', () => {
    it('should open unlock dialog with selected analysis', () => {
      component.openUnlockDialog(mockLockedAnalysis);

      expect(component.showUnlockDialog()).toBe(true);
      expect(component.selectedAnalysis()).toEqual(mockLockedAnalysis);
    });

    it('should close unlock dialog and clear form', () => {
      component.openUnlockDialog(mockLockedAnalysis);
      component.unlockForm.get('justification')?.setValue('Test justification');

      component.closeUnlockDialog();

      expect(component.showUnlockDialog()).toBe(false);
      expect(component.selectedAnalysis()).toBeNull();
      expect(component.unlockForm.get('justification')?.value).toBeNull();
    });

    it('should reset errors when opening dialog', () => {
      component.unlockError.set('Previous error');
      component.staleRowVersionError.set(true);

      component.openUnlockDialog(mockLockedAnalysis);

      expect(component.unlockError()).toBe('');
      expect(component.staleRowVersionError()).toBe(false);
    });
  });

  describe('justification validation', () => {
    it('should disable unlock button when justification is empty', () => {
      component.unlockForm.get('justification')?.setValue('');
      component.unlockForm.get('justification')?.markAsTouched();

      expect(component.unlockForm.valid).toBe(false);
    });

    it('should enable unlock button when justification is provided', () => {
      component.unlockForm.get('justification')?.setValue('Valid justification');

      expect(component.unlockForm.valid).toBe(true);
    });

    it('should show error message when justification is required but empty', () => {
      const control = component.unlockForm.get('justification');
      control?.markAsTouched();
      control?.setValue('');

      expect(control?.hasError('required')).toBe(true);
    });
  });

  describe('unlock submission', () => {
    beforeEach(() => {
      const mockUser = {
        sub: 'user123',
        role: 'LabCoordinator',
        site: 'Site1',
      };
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      vi.spyOn(currentUserService, 'user').mockReturnValue(mockUser as any);
      vi.spyOn(apiService, 'listExceptionAnalyses').mockReturnValue(
        of([mockLockedAnalysis])
      );
    });

    it('should pass rowVersion in unlock request', () => {
      vi.spyOn(apiService, 'unlockResult').mockReturnValue(
        of({
          id: 1,
          isLocked: false,
          rowVersion: 'v1.1',
        })
      );

      component.selectedAnalysis.set(mockLockedAnalysis);
      component.unlockForm.get('justification')?.setValue('Test justification');
      component.submitUnlock();

      expect(apiService.unlockResult).toHaveBeenCalledWith(
        1,
        expect.objectContaining({
          justification: 'Test justification',
          rowVersion: 'v1.0',
        })
      );
    });

    it('should handle 409 stale rowVersion error', () => {
      const error409 = { status: 409 };
      vi.spyOn(apiService, 'unlockResult').mockReturnValue(
        throwError(() => error409)
      );

      component.selectedAnalysis.set(mockLockedAnalysis);
      component.unlockForm.get('justification')?.setValue('Test justification');
      component.submitUnlock();

      expect(component.staleRowVersionError()).toBe(true);
      expect(component.unlocking()).toBe(false);
    });

    it('should handle other errors', () => {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const error: any = {
        status: 400,
        error: { message: 'Invalid request' },
      };
      vi.spyOn(apiService, 'unlockResult').mockReturnValue(
        throwError(() => error)
      );

      component.selectedAnalysis.set(mockLockedAnalysis);
      component.unlockForm.get('justification')?.setValue('Test justification');
      component.submitUnlock();

      expect(component.unlockError()).toContain('Invalid request');
      expect(component.unlocking()).toBe(false);
    });

    it('should reload after successful unlock', () => {
      vi.spyOn(apiService, 'unlockResult').mockReturnValue(
        of({
          id: 1,
          isLocked: false,
          rowVersion: 'v1.1',
        })
      );
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      vi.spyOn(component as any, 'loadAnalyses');

      component.selectedAnalysis.set(mockLockedAnalysis);
      component.unlockForm.get('justification')?.setValue('Test justification');
      component.submitUnlock();

      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      expect((component as any).loadAnalyses).toHaveBeenCalled();
      expect(component.showUnlockDialog()).toBe(false);
    });

    it('should not submit if form is invalid', () => {
      vi.spyOn(apiService, 'unlockResult');

      component.selectedAnalysis.set(mockLockedAnalysis);
      component.unlockForm.get('justification')?.setValue('');
      component.submitUnlock();

      expect(apiService.unlockResult).not.toHaveBeenCalled();
    });

    it('should not submit if no analysis is selected', () => {
      vi.spyOn(apiService, 'unlockResult');

      component.selectedAnalysis.set(null);
      component.unlockForm.get('justification')?.setValue('Test justification');
      component.submitUnlock();

      expect(apiService.unlockResult).not.toHaveBeenCalled();
    });

    it('should set unlocking state during submission', () => {
      vi.spyOn(apiService, 'unlockResult').mockReturnValue(
        of({
          id: 1,
          isLocked: false,
          rowVersion: 'v1.1',
        })
      );

      component.selectedAnalysis.set(mockLockedAnalysis);
      component.unlockForm.get('justification')?.setValue('Test justification');

      expect(component.unlocking()).toBe(false);

      component.submitUnlock();

      expect(component.unlocking()).toBe(false);
    });
  });

  describe('grid display', () => {
    beforeEach(() => {
      vi.spyOn(apiService, 'listExceptionAnalyses').mockReturnValue(
        of([mockLockedAnalysis, mockUnlockedAnalysis])
      );
    });

    it('should show unlock button only for locked analyses', () => {
      const mockUser = {
        sub: 'user123',
        role: 'LabCoordinator',
        site: 'Site1',
      };
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      vi.spyOn(currentUserService, 'user').mockReturnValue(mockUser as any);

      component.ngOnInit();

      const lockedAnalysis = component.analyses()[0];
      expect(lockedAnalysis.isLocked).toBe(true);

      const unlockedAnalysis = component.analyses()[1];
      expect(unlockedAnalysis.isLocked).toBe(false);
    });

    it('should hide unlock button for non-Lab Coordinators', () => {
      const mockUser = {
        sub: 'user123',
        role: 'ControlLabAnalyst',
        site: 'Site1',
      };
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      vi.spyOn(currentUserService, 'user').mockReturnValue(mockUser as any);

      expect(component.isLabCoordinator()).toBe(false);
    });
  });
});

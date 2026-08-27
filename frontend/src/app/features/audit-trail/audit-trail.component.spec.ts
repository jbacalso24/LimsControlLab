import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AuditTrailComponent } from './audit-trail.component';
import {
  AuditLogDto,
  AuditLogPageDto,
  AuditTrailApiService,
} from './services/audit-trail-api.service';

describe('AuditTrailComponent', () => {
  let component: AuditTrailComponent;
  let fixture: ComponentFixture<AuditTrailComponent>;
  let apiService: AuditTrailApiService;

  const mockLog: AuditLogDto = {
    id: 1,
    userId: 10,
    username: 'jsmith',
    role: 'LabCoordinator',
    timestampUtc: '2026-08-20T10:00:00Z',
    action: 'Update',
    entityType: 'Sample',
    entityId: 55,
    beforeValues: '{"status":"Draft"}',
    afterValues: '{"status":"Complete"}',
    correlationId: 'corr-1',
  };

  const pageOf = (items: AuditLogDto[], total = items.length): AuditLogPageDto => ({
    items,
    total,
    page: 1,
    pageSize: 25,
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuditTrailComponent],
      providers: [AuditTrailApiService, provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(AuditTrailComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(AuditTrailApiService);
  });

  describe('initialization', () => {
    it('should load audit logs on init', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([mockLog])));

      component.ngOnInit();

      expect(component.items()).toEqual([mockLog]);
      expect(component.total()).toBe(1);
      expect(component.loading()).toBe(false);
    });

    it('should display loading state while fetching', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([])));

      component.loading.set(true);
      expect(component.loading()).toBe(true);
    });
  });

  describe('state handling', () => {
    it('should display error message on load failure', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(
        throwError(() => ({ status: 500 }))
      );

      component.ngOnInit();

      expect(component.error()).toContain('Failed to load');
      expect(component.loading()).toBe(false);
    });

    it('should set forbidden on 403', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(
        throwError(() => ({ status: 403 }))
      );

      component.ngOnInit();

      expect(component.forbidden()).toBe(true);
      expect(component.loading()).toBe(false);
    });

    it('should display empty state when no logs', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([])));

      component.ngOnInit();

      expect(component.items()).toEqual([]);
    });

    it('should reload logs on retry', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([mockLog])));

      component.reload();

      expect(component.items()).toEqual([mockLog]);
    });
  });

  describe('filtering', () => {
    it('should reset page to 1 and refetch when entity type filter changes', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([mockLog])));
      component.page.set(3);

      component.onEntityTypeChange('Sample');

      expect(component.page()).toBe(1);
      expect(apiService.listAuditLogs).toHaveBeenCalledWith(
        expect.objectContaining({ entityType: 'Sample', page: 1 })
      );
    });

    it('should reset page to 1 and refetch when action filter changes', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([mockLog])));
      component.page.set(2);

      component.onActionChange('Update');

      expect(component.page()).toBe(1);
      expect(apiService.listAuditLogs).toHaveBeenCalledWith(
        expect.objectContaining({ action: 'Update', page: 1 })
      );
    });

    it('should omit empty filters from the request', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([mockLog])));

      component.onEntityTypeChange('');

      expect(apiService.listAuditLogs).toHaveBeenCalledWith(
        expect.objectContaining({ entityType: undefined })
      );
    });
  });

  describe('pagination', () => {
    it('should refetch with new page on page change', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([mockLog], 100)));

      component.onPageChange(2);

      expect(component.page()).toBe(2);
      expect(apiService.listAuditLogs).toHaveBeenCalledWith(
        expect.objectContaining({ page: 2 })
      );
    });

    it('should compute total pages from total and page size', () => {
      vi.spyOn(apiService, 'listAuditLogs').mockReturnValue(of(pageOf([mockLog], 60)));

      component.ngOnInit();

      expect(component.totalPages()).toBe(3);
    });
  });

  describe('display helpers', () => {
    it('should format the entity label', () => {
      expect(component.entityLabel(mockLog)).toBe('Sample #55');
    });

    it('should format the changes summary with before and after values', () => {
      expect(component.changesSummary(mockLog)).toBe(
        '{"status":"Draft"} -> {"status":"Complete"}'
      );
    });

    it('should show a dash when both before and after values are null', () => {
      const log: AuditLogDto = { ...mockLog, beforeValues: null, afterValues: null };
      expect(component.changesSummary(log)).toBe('-');
    });
  });
});

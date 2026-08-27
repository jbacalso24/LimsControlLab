import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { WorkQueueComponent } from './work-queue.component';
import { WorkQueueApiService } from './services/work-queue-api.service';
import { SearchResultItemDto } from '../../shared/generated/models/search-result-item-dto';
import { PagedResultOfSearchResultItemDto } from '../../shared/generated/models/paged-result-of-search-result-item-dto';
import { ScheduleAdherenceItem, ScheduleAdherenceResponse } from './services/schedule-adherence.models';
import { of, throwError } from 'rxjs';

const emptyWorkQueue: PagedResultOfSearchResultItemDto = {
  items: [],
  pageNumber: 1,
  pageSize: 10,
  totalCount: 0,
};

const emptyAdherence: ScheduleAdherenceResponse = {
  asOfUtc: '2026-08-27T00:00:00Z',
  summary: { onTrack: 0, due: 0, overdue: 0, missed: 0, total: 0 },
  schedules: [],
};

describe('WorkQueueComponent', () => {
  let component: WorkQueueComponent;
  let service: WorkQueueApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [WorkQueueComponent, HttpClientTestingModule],
      providers: [WorkQueueApiService, provideRouter([])],
    });

    component = TestBed.createComponent(WorkQueueComponent).componentInstance;
    service = TestBed.inject(WorkQueueApiService);
    vi.spyOn(service, 'getAdherence').mockReturnValue(of(emptyAdherence));
  });

  it('should bucket results by status correctly', () => {
    const mockItems: SearchResultItemDto[] = [
      {
        analysisId: 1,
        sampleId: 1,
        sampleIdentifier: 'SAMPLE-001',
        site: 'PIONEER',
        startedAtUtc: '2026-08-26T10:00:00Z',
        status: 'NotStarted',
        templateName: 'Pol Test',
        isLocked: false,
      },
      {
        analysisId: 2,
        sampleId: 2,
        sampleIdentifier: 'SAMPLE-002',
        site: 'INKERMAN',
        startedAtUtc: '2026-08-26T11:00:00Z',
        status: 'InProgress',
        templateName: 'Brix Test',
        isLocked: false,
      },
      {
        analysisId: 3,
        sampleId: 3,
        sampleIdentifier: 'SAMPLE-003',
        site: 'INVICTA',
        startedAtUtc: '2026-08-26T09:00:00Z',
        status: 'OnHold',
        templateName: 'Water Test',
        isLocked: false,
      },
      {
        analysisId: 4,
        sampleId: 4,
        sampleIdentifier: 'SAMPLE-004',
        site: 'KALAMIA',
        startedAtUtc: '2026-08-26T08:00:00Z',
        status: 'Completed',
        templateName: 'RS Test',
        isLocked: true,
      },
      {
        analysisId: 5,
        sampleId: 5,
        sampleIdentifier: 'SAMPLE-005',
        site: 'VICTORIA',
        startedAtUtc: '2026-08-26T07:00:00Z',
        status: 'Cancelled',
        templateName: 'Ash Test',
        isLocked: false,
      },
    ];

    const mockResponse: PagedResultOfSearchResultItemDto = {
      items: mockItems,
      pageNumber: 1,
      pageSize: 10,
      totalCount: 5,
    };

    vi.spyOn(service, 'getWorkQueue').mockReturnValue(of(mockResponse));

    component.ngOnInit();

    const columns = component.columns();
    expect(columns.length).toBe(3);

    expect(columns[0].title).toBe('Not Started');
    expect(columns[0].items.length).toBe(1);
    expect(columns[0].items[0].analysisId).toBe(1);

    expect(columns[1].title).toBe('In Progress');
    expect(columns[1].items.length).toBe(2);
    const inProgressIds = columns[1].items.map((item) => item.analysisId);
    expect(inProgressIds).toContain(2);
    expect(inProgressIds).toContain(3);

    expect(columns[2].title).toBe('Completed');
    expect(columns[2].items.length).toBe(1);
    expect(columns[2].items[0].analysisId).toBe(4);

    const cancelledItem = mockItems.find((item) => item.status === 'Cancelled');
    const allBucketedItems = columns
      .flatMap((column) => column.items)
      .map((item) => item.analysisId);
    expect(allBucketedItems).not.toContain(cancelledItem!.analysisId);
  });

  it('should handle empty work queue', () => {
    const mockResponse: PagedResultOfSearchResultItemDto = {
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    };

    vi.spyOn(service, 'getWorkQueue').mockReturnValue(of(mockResponse));

    component.ngOnInit();

    const columns = component.columns();
    expect(columns.length).toBe(3);
    columns.forEach((column) => {
      expect(column.items.length).toBe(0);
    });
  });

  it('should load work queue on init', () => {
    const mockResponse: PagedResultOfSearchResultItemDto = {
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    };

    vi.spyOn(service, 'getWorkQueue').mockReturnValue(of(mockResponse));

    expect(component.loading()).toBe(false);
    component.ngOnInit();
    expect(service.getWorkQueue).toHaveBeenCalled();
  });

  it('should handle error loading work queue', () => {
    vi.spyOn(service, 'getWorkQueue').mockReturnValue(
      throwError(() => new Error('API error'))
    );

    component.ngOnInit();

    expect(component.error()).toBeTruthy();
  });

  it('should reload work queue', () => {
    const mockResponse: PagedResultOfSearchResultItemDto = {
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    };

    vi.spyOn(service, 'getWorkQueue').mockReturnValue(of(mockResponse));

    component.reload();

    expect(service.getWorkQueue).toHaveBeenCalled();
  });

  describe('schedule adherence', () => {
    beforeEach(() => {
      vi.spyOn(service, 'getWorkQueue').mockReturnValue(of(emptyWorkQueue));
    });

    it('loads adherence summary and schedules on init', () => {
      const mockSchedules: ScheduleAdherenceItem[] = [
        {
          scheduleId: 1,
          name: 'Hourly Brix',
          analysisType: 'Brix',
          shiftPattern: 'Shift',
          cadenceLabel: 'Every shift',
          status: 'Overdue',
          assignedToUserId: 5,
          assignedToUsername: 'jsmith',
          lastAnalysisAtUtc: '2026-08-26T08:00:00Z',
          missedPeriods: 0,
          currentPeriodStartUtc: '2026-08-27T00:00:00Z',
          currentPeriodEndUtc: '2026-08-27T08:00:00Z',
        },
        {
          scheduleId: 2,
          name: 'Daily Pol',
          analysisType: 'Pol',
          shiftPattern: 'Day',
          cadenceLabel: 'Daily',
          status: 'OnTrack',
          assignedToUserId: null,
          assignedToUsername: null,
          lastAnalysisAtUtc: '2026-08-27T06:00:00Z',
          missedPeriods: 0,
          currentPeriodStartUtc: '2026-08-27T00:00:00Z',
          currentPeriodEndUtc: '2026-08-28T00:00:00Z',
        },
      ];
      const mockResponse: ScheduleAdherenceResponse = {
        asOfUtc: '2026-08-27T09:00:00Z',
        summary: { onTrack: 1, due: 0, overdue: 1, missed: 0, total: 2 },
        schedules: mockSchedules,
      };

      vi.spyOn(service, 'getAdherence').mockReturnValue(of(mockResponse));

      component.ngOnInit();

      expect(component.adherenceSummary()).toEqual(mockResponse.summary);
      expect(component.adherence().length).toBe(2);
      expect(component.attentionSchedules().length).toBe(1);
      expect(component.attentionSchedules()[0].scheduleId).toBe(1);
    });

    it('handles an empty adherence payload', () => {
      vi.spyOn(service, 'getAdherence').mockReturnValue(of(emptyAdherence));

      component.ngOnInit();

      expect(component.adherenceSummary().total).toBe(0);
      expect(component.attentionSchedules().length).toBe(0);
      expect(component.adherenceError()).toBe('');
    });

    it('handles an adherence load error', () => {
      vi.spyOn(service, 'getAdherence').mockReturnValue(throwError(() => new Error('API error')));

      component.ngOnInit();

      expect(component.adherenceError()).toBeTruthy();
      expect(component.adherenceLoading()).toBe(false);
    });

    it('reloads adherence independently', () => {
      vi.spyOn(service, 'getAdherence').mockReturnValue(of(emptyAdherence));

      component.reloadAdherence();

      expect(service.getAdherence).toHaveBeenCalled();
    });

    it('renders adherence summary tiles and attention rows', () => {
      const mockSchedules: ScheduleAdherenceItem[] = [
        {
          scheduleId: 3,
          name: 'Weekly Ash',
          analysisType: 'Ash',
          shiftPattern: 'Weekly',
          cadenceLabel: 'Weekly',
          status: 'Missed',
          assignedToUserId: null,
          assignedToUsername: null,
          lastAnalysisAtUtc: null,
          missedPeriods: 2,
          currentPeriodStartUtc: '2026-08-20T00:00:00Z',
          currentPeriodEndUtc: '2026-08-27T00:00:00Z',
        },
      ];
      const mockResponse: ScheduleAdherenceResponse = {
        asOfUtc: '2026-08-27T09:00:00Z',
        summary: { onTrack: 0, due: 0, overdue: 0, missed: 1, total: 1 },
        schedules: mockSchedules,
      };

      vi.spyOn(service, 'getAdherence').mockReturnValue(of(mockResponse));

      const fixture = TestBed.createComponent(WorkQueueComponent);
      fixture.detectChanges();

      const html = fixture.nativeElement as HTMLElement;
      expect(html.textContent).toContain('Missed');
      expect(html.textContent).toContain('Weekly Ash');
      expect(html.textContent).toContain('Unassigned');
      expect(html.textContent).toContain('Never');
    });
  });
});

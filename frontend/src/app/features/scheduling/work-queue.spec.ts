import { describe, it, expect, beforeEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { WorkQueueComponent } from './work-queue.component';
import { WorkQueueApiService } from './services/work-queue-api.service';
import { SearchResultItemDto } from '../../shared/generated/models/search-result-item-dto';
import { PagedResultOfSearchResultItemDto } from '../../shared/generated/models/paged-result-of-search-result-item-dto';
import { of, throwError } from 'rxjs';

describe('WorkQueueComponent', () => {
  let component: WorkQueueComponent;
  let service: WorkQueueApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [WorkQueueComponent, HttpClientTestingModule],
      providers: [WorkQueueApiService],
    });

    component = TestBed.createComponent(WorkQueueComponent).componentInstance;
    service = TestBed.inject(WorkQueueApiService);
  });

  it('should bucket results by status correctly', () => {
    const mockItems: SearchResultItemDto[] = [
      {
        analysisId: 1,
        sampleId: 1,
        sampleIdentifier: 'SAMPLE-001',
        site: 'Site1',
        startedAtUtc: '2026-08-26T00:00:00Z',
        status: 'NotStarted',
        templateName: 'Template1',
        isLocked: false,
      },
      {
        analysisId: 2,
        sampleId: 2,
        sampleIdentifier: 'SAMPLE-002',
        site: 'Site1',
        startedAtUtc: '2026-08-26T00:00:00Z',
        status: 'InProgress',
        templateName: 'Template1',
        isLocked: false,
      },
      {
        analysisId: 3,
        sampleId: 3,
        sampleIdentifier: 'SAMPLE-003',
        site: 'Site1',
        startedAtUtc: '2026-08-26T00:00:00Z',
        status: 'OnHold',
        templateName: 'Template2',
        isLocked: false,
      },
      {
        analysisId: 4,
        sampleId: 4,
        sampleIdentifier: 'SAMPLE-004',
        site: 'Site1',
        startedAtUtc: '2026-08-26T00:00:00Z',
        status: 'Completed',
        templateName: 'Template1',
        isLocked: true,
      },
      {
        analysisId: 5,
        sampleId: 5,
        sampleIdentifier: 'SAMPLE-005',
        site: 'Site1',
        startedAtUtc: '2026-08-26T00:00:00Z',
        status: 'Cancelled',
        templateName: 'Template3',
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
});

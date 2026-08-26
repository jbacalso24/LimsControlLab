import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HistorySearchComponent } from './history-search.component';

describe('HistorySearchComponent', () => {
  let component: HistorySearchComponent;
  let fixture: ComponentFixture<HistorySearchComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HistorySearchComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(HistorySearchComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty filters and load results on ngOnInit', () => {
    fixture.detectChanges();

    expect(component.filterForm.get('templateName')?.value).toBe('');
    expect(component.loading()).toBe(true);
    expect(component.searched()).toBe(true);

    const req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    expect(req.request.method).toBe('POST');
    req.flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    });

    fixture.detectChanges();
    expect(component.loading()).toBe(false);
  });

  it('should submit search with filter values', () => {
    fixture.detectChanges();

    // Consume the initial load request
    httpMock.expectOne((r) => r.url.includes('/search/results')).flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    });

    component.filterForm.patchValue({
      templateName: 'TestTemplate',
      testId: 1,
      sampleIdentifier: 'Sample123',
    });

    component.search();

    const req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    expect(req.request.body).toEqual({
      templateName: 'TestTemplate',
      testId: 1,
      sampleIdentifier: 'Sample123',
    });
    req.flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    });
  });

  it('should handle search results with data', () => {
    fixture.detectChanges();

    const mockData = {
      items: [
        {
          analysisId: 1,
          sampleId: 1,
          sampleIdentifier: 'Sample001',
          site: 'Site A',
          status: 'InProgress',
          templateName: 'Template1',
          startedAtUtc: '2026-08-26T10:00:00Z',
          isLocked: false,
          testId: 1,
          readingValue: 50.5,
          readingUnit: 'mg/L',
          calibratedValue: 50.2,
          capturedAtUtc: '2026-08-26T10:30:00Z',
          validationResult: 'Valid',
          readingId: 1,
          instrumentId: 1,
        },
      ],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 1,
    };

    httpMock.expectOne((r) => r.url.includes('/search/results')).flush(mockData);

    fixture.detectChanges();
    expect(component.items().length).toBe(1);
    expect(component.items()[0].sampleIdentifier).toBe('Sample001');
    expect(component.totalCount()).toBe(1);
  });

  it('should handle search error', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    req.error(new ProgressEvent('error'));

    fixture.detectChanges();
    expect(component.loading()).toBe(false);
    expect(component.error()).toContain('Failed to load');
  });

  it('should handle pagination', () => {
    fixture.detectChanges();

    httpMock.expectOne((r) => r.url.includes('/search/results')).flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 100,
    });

    fixture.detectChanges();

    component.onPageChange(2);

    const req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    expect(req.request.params.get('pageNumber')).toBe('2');
    req.flush({
      items: [],
      pageNumber: 2,
      pageSize: 10,
      totalCount: 100,
    });
  });

  it('should clear filters and search', () => {
    fixture.detectChanges();

    httpMock.expectOne((r) => r.url.includes('/search/results')).flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    });

    component.filterForm.patchValue({
      templateName: 'TestTemplate',
      testId: 1,
    });

    expect(component.filterForm.get('templateName')?.value).toBe('TestTemplate');

    component.clearFilters();

    expect(component.filterForm.get('templateName')?.value).toBeNull();
    expect(component.filterForm.get('testId')?.value).toBeNull();

    const req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    req.flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    });
  });

  it('should reset page index to 1 when searching', () => {
    fixture.detectChanges();

    // Consume initial search
    let req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    req.flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 100,
    });

    // Simulate being on page 2
    component.currentPageIndex.set(2);

    component.search();

    expect(component.currentPageIndex()).toBe(1);

    req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    expect(req.request.params.get('pageNumber')).toBe('1');
    req.flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 100,
    });
  });

  it('should show searched flag after search', () => {
    fixture.detectChanges();

    expect(component.searched()).toBe(true);

    const req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    req.flush({
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    });

    fixture.detectChanges();
    expect(component.searched()).toBe(true);
  });
});

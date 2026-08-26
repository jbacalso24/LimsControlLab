import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HistorySearchApiService } from './history-search-api.service';
import { SearchResultsRequest } from '../../../shared/generated/models/search-results-request';

describe('HistorySearchApiService', () => {
  let service: HistorySearchApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [HistorySearchApiService],
    });
    service = TestBed.inject(HistorySearchApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should search results with correct query params', () => {
    const request: SearchResultsRequest = {
      templateName: 'TestTemplate',
      testId: 1,
    };
    const pageNumber = 1;
    const pageSize = 50;

    service.searchResults(request, pageNumber, pageSize).subscribe((result) => {
      expect(result.items.length).toBeGreaterThanOrEqual(0);
      expect(result.pageNumber).toBe(1);
      expect(result.pageSize).toBe(50);
    });

    const req = httpMock.expectOne(
      (r) => r.url.includes('/search/results') && r.params.has('pageNumber') && r.params.has('pageSize')
    );
    expect(req.request.method).toBe('POST');
    expect(req.request.params.get('pageNumber')).toBe('1');
    expect(req.request.params.get('pageSize')).toBe('50');
    expect(req.request.body).toEqual(request);

    req.flush({
      items: [],
      pageNumber: 1,
      pageSize: 50,
      totalCount: 0,
    });
  });

  it('should handle empty search request', () => {
    const request: SearchResultsRequest = {};
    const pageNumber = 1;
    const pageSize = 50;

    service.searchResults(request, pageNumber, pageSize).subscribe((result) => {
      expect(result.items.length).toBe(0);
    });

    const req = httpMock.expectOne((r) => r.url.includes('/search/results'));
    expect(req.request.body).toEqual({});
    req.flush({
      items: [],
      pageNumber: 1,
      pageSize: 50,
      totalCount: 0,
    });
  });
});

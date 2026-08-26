import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ExceptionReviewApiService } from './exception-review-api.service';
import { ResultReviewDto } from '../../../shared/generated/models/result-review-dto';
import { UnlockResultRequest } from '../../../shared/generated/models/unlock-result-request';
import { UnlockResultDto } from '../../../shared/generated/models/unlock-result-dto';

describe('ExceptionReviewApiService', () => {
  let service: ExceptionReviewApiService;
  let httpMock: HttpTestingController;

  const mockResultReviewDto: ResultReviewDto = {
    id: 1,
    sampleId: 100,
    sampleIdentifier: 'SMP-100',
    templateId: 50,
    templateName: 'Brix Control',
    site: 'Invicta',
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

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ExceptionReviewApiService],
    });

    service = TestBed.inject(ExceptionReviewApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('listExceptionAnalyses', () => {
    it('should fetch list of exception analyses', () => {
      const mockData: ResultReviewDto[] = [mockResultReviewDto];

      service.listExceptionAnalyses().subscribe((data) => {
        expect(data).toEqual(mockData);
        expect(data[0].rowVersion).toBe('v1.0');
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes('/results/exception-analyses')
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockData);
    });

    it('should handle empty list', () => {
      service.listExceptionAnalyses().subscribe((data) => {
        expect(data).toEqual([]);
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes('/results/exception-analyses')
      );
      req.flush([]);
    });
  });

  describe('unlockResult', () => {
    it('should send PATCH request with unlock request', () => {
      const analysisId = 1;
      const request: UnlockResultRequest = {
        justification: 'Test justification',
        rowVersion: 'v1.0',
      };
      const mockResponse: UnlockResultDto = {
        id: 1,
        isLocked: false,
        rowVersion: 'v1.1',
      };

      service.unlockResult(analysisId, request).subscribe((response) => {
        expect(response).toEqual(mockResponse);
        expect(response.rowVersion).toBe('v1.1');
        expect(response.isLocked).toBe(false);
      });

      const req = httpMock.expectOne((httpRequest) =>
        httpRequest.url.includes(`/results/${analysisId}/unlock`)
      );
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual(request);
      req.flush(mockResponse);
    });

    it('should include rowVersion in request body', () => {
      const analysisId = 1;
      const request: UnlockResultRequest = {
        justification: 'Test justification',
        rowVersion: 'v1.0',
      };

      service.unlockResult(analysisId, request).subscribe();

      const req = httpMock.expectOne((httpRequest) =>
        httpRequest.url.includes(`/results/${analysisId}/unlock`)
      );
      expect(req.request.body.rowVersion).toBe('v1.0');
      req.flush({ id: 1, isLocked: false, rowVersion: 'v1.1' });
    });

    it('should handle 409 conflict response', () => {
      const analysisId = 1;
      const request: UnlockResultRequest = {
        justification: 'Test justification',
        rowVersion: 'v1.0',
      };

      service.unlockResult(analysisId, request).subscribe(
        () => expect.unreachable('should have failed'),
        (error) => {
          expect(error.status).toBe(409);
        }
      );

      const req = httpMock.expectOne((httpRequest) =>
        httpRequest.url.includes(`/results/${analysisId}/unlock`)
      );
      req.flush(
        { message: 'Conflict' },
        { status: 409, statusText: 'Conflict' }
      );
    });
  });
});

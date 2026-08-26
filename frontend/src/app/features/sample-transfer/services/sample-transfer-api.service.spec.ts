import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SampleTransferApiService } from './sample-transfer-api.service';

describe('SampleTransferApiService', () => {
  let service: SampleTransferApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SampleTransferApiService],
    });

    service = TestBed.inject(SampleTransferApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getSample', () => {
    it('should fetch sample by id', () => {
      const mockSample = {
        id: 1,
        identifier: 'SAMPLE-001',
        site: 'Site1',
        currentSite: 'Site1',
        status: 'InProgress',
        analysisTemplateId: 1,
        rowVersion: 'AQAAAAIAAAA=',
      };

      service.getSample(1).subscribe((result) => {
        expect(result).toEqual(mockSample);
      });

      const req = httpMock.expectOne((request) =>
        request.url.includes('/samples/1')
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockSample);
    });

    it('should handle 403 Forbidden error', () => {
      service.getSample(1).subscribe(
        () => expect.unreachable('should have failed'),
        (error) => {
          expect(error.status).toBe(403);
        }
      );

      const req = httpMock.expectOne((request) =>
        request.url.includes('/samples/1')
      );
      req.flush({ error: 'Forbidden' }, { status: 403, statusText: 'Forbidden' });
    });

    it('should handle 404 Not Found error', () => {
      service.getSample(999).subscribe(
        () => expect.unreachable('should have failed'),
        (error) => {
          expect(error.status).toBe(404);
        }
      );

      const req = httpMock.expectOne((request) =>
        request.url.includes('/samples/999')
      );
      req.flush({ error: 'Not Found' }, { status: 404, statusText: 'Not Found' });
    });
  });

  describe('transferSample', () => {
    it('should transfer sample to another site', () => {
      const mockTransfer = {
        id: 1,
        fromSite: 'Site1',
        toSite: 'Site2',
        transferredAtUtc: '2026-08-26T10:30:00Z',
        rowVersion: 'AQAAAAMAAAA=',
      };

      const request = {
        toSite: 'Site2',
        rowVersion: 'AQAAAAIAAAA=',
      };

      service.transferSample(1, request).subscribe((result) => {
        expect(result).toEqual(mockTransfer);
      });

      const req = httpMock.expectOne((httpRequest) =>
        httpRequest.url.includes('/samples/1/transfer')
      );
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(request);
      req.flush(mockTransfer);
    });

    it('should handle 409 Conflict on stale rowVersion', () => {
      const request = {
        toSite: 'Site2',
        rowVersion: 'STALE_VERSION',
      };

      service.transferSample(1, request).subscribe(
        () => expect.unreachable('should have failed'),
        (error) => {
          expect(error.status).toBe(409);
        }
      );

      const req = httpMock.expectOne((httpRequest) =>
        httpRequest.url.includes('/samples/1/transfer')
      );
      req.flush({ error: 'Conflict' }, { status: 409, statusText: 'Conflict' });
    });

    it('should handle 400 Bad Request error', () => {
      const request = {
        toSite: 'Site2',
        rowVersion: 'AQAAAAIAAAA=',
      };

      service.transferSample(1, request).subscribe(
        () => expect.unreachable('should have failed'),
        (error) => {
          expect(error.status).toBe(400);
        }
      );

      const req = httpMock.expectOne((httpRequest) =>
        httpRequest.url.includes('/samples/1/transfer')
      );
      req.flush({ error: 'Bad Request' }, { status: 400, statusText: 'Bad Request' });
    });
  });
});

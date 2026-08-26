import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { SampleDto } from '../../../shared/generated/models/sample-dto';
import { TransferSampleRequest } from '../../../shared/generated/models/transfer-sample-request';
import { SampleTransferDto } from '../../../shared/generated/models/sample-transfer-dto';
import { PagedResultOfSearchResultItemDto } from '../../../shared/generated/models/paged-result-of-search-result-item-dto';

@Injectable({
  providedIn: 'root',
})
export class SampleTransferApiService extends LimsApiService {
  getSample(sampleId: number): Observable<SampleDto> {
    return this.get<SampleDto>(`/samples/${sampleId}`);
  }

  /**
   * There is no dedicated "list samples" endpoint, so the picker reuses the
   * search endpoint (scoped to the caller's site) and de-dupes by sample.
   */
  listSamplesForPicker(): Observable<PagedResultOfSearchResultItemDto> {
    return this.post<PagedResultOfSearchResultItemDto>('/search/results', {}, { pageNumber: 1, pageSize: 200 });
  }

  transferSample(sampleId: number, request: TransferSampleRequest): Observable<SampleTransferDto> {
    return this.post<SampleTransferDto>(`/samples/${sampleId}/transfer`, request);
  }
}

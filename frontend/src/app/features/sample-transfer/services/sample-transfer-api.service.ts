import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { SampleDto } from '../../../shared/generated/models/sample-dto';
import { TransferSampleRequest } from '../../../shared/generated/models/transfer-sample-request';
import { SampleTransferDto } from '../../../shared/generated/models/sample-transfer-dto';

@Injectable({
  providedIn: 'root',
})
export class SampleTransferApiService extends LimsApiService {
  getSample(sampleId: number): Observable<SampleDto> {
    return this.get<SampleDto>(`/samples/${sampleId}`);
  }

  transferSample(sampleId: number, request: TransferSampleRequest): Observable<SampleTransferDto> {
    return this.post<SampleTransferDto>(`/samples/${sampleId}/transfer`, request);
  }
}

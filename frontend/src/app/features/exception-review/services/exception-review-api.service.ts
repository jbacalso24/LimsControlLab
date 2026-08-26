import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { ResultReviewDto } from '../../../shared/generated/models/result-review-dto';
import { UnlockResultRequest } from '../../../shared/generated/models/unlock-result-request';
import { UnlockResultDto } from '../../../shared/generated/models/unlock-result-dto';

@Injectable({
  providedIn: 'root',
})
export class ExceptionReviewApiService extends LimsApiService {
  listExceptionAnalyses(): Observable<ResultReviewDto[]> {
    return this.get<ResultReviewDto[]>('/results/exception-analyses');
  }

  unlockResult(analysisId: number, request: UnlockResultRequest): Observable<UnlockResultDto> {
    return this.patch<UnlockResultDto>(`/results/${analysisId}/unlock`, request);
  }
}

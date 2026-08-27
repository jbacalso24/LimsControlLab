import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { ResultComparisonRequest, ResultComparisonResponse } from './result-comparison.models';

@Injectable({
  providedIn: 'root',
})
export class ResultComparisonApiService extends LimsApiService {
  compare(request: ResultComparisonRequest): Observable<ResultComparisonResponse> {
    return this.post<ResultComparisonResponse>('/results/comparison', request);
  }
}

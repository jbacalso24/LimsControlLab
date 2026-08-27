import { Injectable } from '@angular/core';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { Observable } from 'rxjs';
import { PagedResultOfSearchResultItemDto } from '../../../shared/generated/models/paged-result-of-search-result-item-dto';
import { ScheduleAdherenceResponse } from './schedule-adherence.models';

@Injectable({
  providedIn: 'root',
})
export class WorkQueueApiService extends LimsApiService {
  getWorkQueue(): Observable<PagedResultOfSearchResultItemDto> {
    return this.post<PagedResultOfSearchResultItemDto>(
      '/search/results',
      {}
    );
  }

  getAdherence(): Observable<ScheduleAdherenceResponse> {
    return this.get<ScheduleAdherenceResponse>('/schedules/adherence');
  }
}

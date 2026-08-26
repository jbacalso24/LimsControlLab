import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { SearchResultsRequest } from '../../../shared/generated/models/search-results-request';
import { PagedResultOfSearchResultItemDto } from '../../../shared/generated/models/paged-result-of-search-result-item-dto';

@Injectable({
  providedIn: 'root',
})
export class HistorySearchApiService extends LimsApiService {
  searchResults(
    request: SearchResultsRequest,
    pageNumber: number,
    pageSize: number
  ): Observable<PagedResultOfSearchResultItemDto> {
    return this.post<PagedResultOfSearchResultItemDto>('/search/results', request, {
      pageNumber,
      pageSize,
    });
  }
}

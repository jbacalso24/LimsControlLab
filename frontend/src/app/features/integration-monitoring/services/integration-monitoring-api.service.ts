import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';

export type IntegrationTargetSystem = 'Databank' | 'SCADA' | 'DataLakehouse';
export type IntegrationLogStatus = 'Pending' | 'Success' | 'Failed';

export interface IntegrationLogDto {
  id: number;
  targetSystem: IntegrationTargetSystem;
  analysisId: number;
  status: IntegrationLogStatus;
  attemptedAtUtc: string;
  completedAtUtc: string | null;
  errorMessage: string | null;
  retryCount: number;
}

export interface ReprocessResultDto {
  id: number;
  status: string;
  success: boolean;
  message: string;
}

export interface IntegrationLogFilter {
  status?: string;
  targetSystem?: string;
}

@Injectable({
  providedIn: 'root',
})
export class IntegrationMonitoringApiService extends LimsApiService {
  listLogs(filter: IntegrationLogFilter): Observable<IntegrationLogDto[]> {
    const params: Record<string, string> = {};
    if (filter.status) params['status'] = filter.status;
    if (filter.targetSystem) params['targetSystem'] = filter.targetSystem;
    return this.get<IntegrationLogDto[]>('/integration-logs', params);
  }

  reprocess(id: number): Observable<ReprocessResultDto> {
    return this.post<ReprocessResultDto>(`/integration-logs/${id}/reprocess`);
  }
}

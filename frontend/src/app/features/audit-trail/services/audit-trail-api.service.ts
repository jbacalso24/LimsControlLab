import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';

export interface AuditLogDto {
  id: number;
  userId: number;
  username: string;
  role: string;
  timestampUtc: string;
  action: string;
  entityType: string;
  entityId: number;
  beforeValues: string | null;
  afterValues: string | null;
  correlationId: string | null;
}

export interface AuditLogPageDto {
  items: AuditLogDto[];
  total: number;
  page: number;
  pageSize: number;
}

export interface ListAuditLogsParams {
  entityType?: string;
  action?: string;
  page: number;
  pageSize: number;
}

@Injectable({
  providedIn: 'root',
})
export class AuditTrailApiService extends LimsApiService {
  listAuditLogs(params: ListAuditLogsParams): Observable<AuditLogPageDto> {
    const query: Record<string, unknown> = {
      page: params.page,
      pageSize: params.pageSize,
    };
    if (params.entityType) {
      query['entityType'] = params.entityType;
    }
    if (params.action) {
      query['action'] = params.action;
    }
    return this.get<AuditLogPageDto>('/audit-logs', query);
  }
}

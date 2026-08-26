import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from './lims/lims-api.service';

export interface ResetDataResult {
  message: string;
  users: number;
  instruments: number;
  samplingMethods: number;
  analysisTemplates: number;
  schedules: number;
  samples: number;
  analyses: number;
  readings: number;
  exceptionRecords: number;
  calibrationCurves: number;
  sampleTransfers: number;
  integrationLogs: number;
}

/**
 * Development-only admin operations. The reset endpoint is a no-op (404) outside
 * the Development environment on the server, so this is safe to expose in the UI.
 */
@Injectable({ providedIn: 'root' })
export class AdminApiService extends LimsApiService {
  resetData(): Observable<ResetDataResult> {
    return this.post<ResetDataResult>('/admin/reset-data', {});
  }
}

import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';

export interface CalibrationPointDto {
  xValue: number | string;
  yValue: number | string;
}

export interface CalibrationCurveDto {
  id: number | string;
  name: string;
  analysisTemplateId: number | string;
  templateName: string;
  site: string;
  isActive: boolean;
  points: CalibrationPointDto[];
  rowVersion: string;
}

@Injectable({
  providedIn: 'root',
})
export class CalibrationCurvesApiService extends LimsApiService {
  listCurves(): Observable<CalibrationCurveDto[]> {
    return this.get<CalibrationCurveDto[]>('/calibration-curves');
  }
}

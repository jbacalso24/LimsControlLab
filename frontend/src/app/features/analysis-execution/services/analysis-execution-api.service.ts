import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { CreateReadingRequest } from '../../../shared/generated/models/create-reading-request';
import { ExceptionDecisionRequest } from '../../../shared/generated/models/exception-decision-request';
import { StatusChangeRequest } from '../../../shared/generated/models/status-change-request';

export interface AnalysisDetailDto {
  id: number;
  sampleId: number;
  templateId: number;
  status: string;
  isLocked: boolean;
  readings: ReadingDto[];
  exceptions: ExceptionDto[];
  rowVersion: string;
  availableTests: TestDefinitionDto[];
}

export interface TestDefinitionDto {
  id: number | string;
  name: string;
  unit: string;
  method?: string;
}

export interface ReadingDto {
  id: number;
  testId: number | string;
  value: number;
  unit: string;
  capturedAtUtc: string;
  capturedBy: string;
  capturedByUsername: string;
  validationResult: {
    isValid: boolean;
    expectedRange?: string;
    actualValue: string;
    reason?: string;
  };
}

export interface ExceptionDto {
  id: number;
  readingId: number;
  reason: string;
  decision?: 'Modify' | 'Retest' | 'AcceptWithComment' | null;
  decisionComment?: string | null;
  rowVersion: string;
}

export interface InstrumentDto {
  id: number;
  name: string;
  model?: string;
  site: string;
  isActive: boolean;
}

export interface CreateAnalysisRequest {
  analysisTemplateId: number;
  sampleIdentifier?: string | null;
}

export interface CreatedAnalysisDto {
  analysisId: number;
  sampleId: number;
  sampleIdentifier: string;
}

@Injectable({
  providedIn: 'root',
})
export class AnalysisExecutionApiService extends LimsApiService {
  getAnalysis(analysisId: number): Observable<AnalysisDetailDto> {
    return this.get<AnalysisDetailDto>(`/analyses/${analysisId}`);
  }

  addReading(
    analysisId: number,
    request: CreateReadingRequest
  ): Observable<ReadingDto> {
    return this.post<ReadingDto>(`/analyses/${analysisId}/readings`, request);
  }

  resolveException(
    analysisId: number,
    exceptionId: number,
    request: ExceptionDecisionRequest
  ): Observable<ExceptionDto> {
    return this.post<ExceptionDto>(
      `/analyses/${analysisId}/exceptions/${exceptionId}/decision`,
      request
    );
  }

  changeStatus(
    analysisId: number,
    request: StatusChangeRequest
  ): Observable<AnalysisDetailDto> {
    return this.patch<AnalysisDetailDto>(
      `/analyses/${analysisId}/status`,
      request
    );
  }

  getInstruments(): Observable<InstrumentDto[]> {
    return this.get<InstrumentDto[]>(`/instruments`);
  }

  createAnalysis(request: CreateAnalysisRequest): Observable<CreatedAnalysisDto> {
    return this.post<CreatedAnalysisDto>('/analyses', request);
  }
}

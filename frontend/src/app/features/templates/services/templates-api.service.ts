import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LimsApiService } from '../../../shared/services/api/lims/lims-api.service';
import { AnalysisTemplateDto } from '../../../shared/generated/models/analysis-template-dto';
import { CreateAnalysisTemplateRequest } from '../../../shared/generated/models/create-analysis-template-request';
import { UpdateAnalysisTemplateRequest } from '../../../shared/generated/models/update-analysis-template-request';

@Injectable({
  providedIn: 'root',
})
export class TemplatesApiService extends LimsApiService {
  listTemplates(): Observable<AnalysisTemplateDto[]> {
    return this.get<AnalysisTemplateDto[]>('/analysis-templates');
  }

  getTemplate(id: number): Observable<AnalysisTemplateDto> {
    return this.get<AnalysisTemplateDto>(`/analysis-templates/${id}`);
  }

  createTemplate(request: CreateAnalysisTemplateRequest): Observable<AnalysisTemplateDto> {
    return this.post<AnalysisTemplateDto>('/analysis-templates', request);
  }

  updateTemplate(
    id: number,
    request: UpdateAnalysisTemplateRequest
  ): Observable<AnalysisTemplateDto> {
    return this.put<AnalysisTemplateDto>(`/analysis-templates/${id}`, request);
  }

  retireTemplate(id: number): Observable<void> {
    return this.post<void>(`/analysis-templates/${id}/retire`, {});
  }
}

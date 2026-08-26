import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { describe, it, beforeEach, afterEach, expect } from 'vitest';
import { TemplatesApiService } from './templates-api.service';
import { AnalysisTemplateDto } from '../../../shared/generated/models/analysis-template-dto';
import { CreateAnalysisTemplateRequest } from '../../../shared/generated/models/create-analysis-template-request';
import { UpdateAnalysisTemplateRequest } from '../../../shared/generated/models/update-analysis-template-request';

describe('TemplatesApiService', () => {
  let service: TemplatesApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [TemplatesApiService],
    });

    service = TestBed.inject(TemplatesApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should list templates', () => {
    const mockTemplates: AnalysisTemplateDto[] = [
      {
        id: 1,
        name: 'Template 1',
        site: 'Site1',
        version: 1,
        isRetired: false,
        rowVersion: 'v1',
      },
    ];

    service.listTemplates().subscribe((result) => {
      expect(result).toEqual(mockTemplates);
    });

    const req = httpMock.expectOne('/analysis-templates');
    expect(req.request.method).toBe('GET');
    req.flush(mockTemplates);
  });

  it('should get a template', () => {
    const mockTemplate: AnalysisTemplateDto = {
      id: 1,
      name: 'Template 1',
      site: 'Site1',
      version: 1,
      isRetired: false,
      rowVersion: 'v1',
    };

    service.getTemplate(1).subscribe((result) => {
      expect(result).toEqual(mockTemplate);
    });

    const req = httpMock.expectOne('/analysis-templates/1');
    expect(req.request.method).toBe('GET');
    req.flush(mockTemplate);
  });

  it('should create a template', () => {
    const request: CreateAnalysisTemplateRequest = {
      name: 'New Template',
      site: 'Site1',
    };
    const mockTemplate: AnalysisTemplateDto = {
      id: 1,
      ...request,
      version: 1,
      isRetired: false,
      rowVersion: 'v1',
    };

    service.createTemplate(request).subscribe((result) => {
      expect(result).toEqual(mockTemplate);
    });

    const req = httpMock.expectOne('/analysis-templates');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockTemplate);
  });

  it('should update a template', () => {
    const request: UpdateAnalysisTemplateRequest = {
      name: 'Updated Template',
      rowVersion: 'v1',
    };
    const mockTemplate: AnalysisTemplateDto = {
      id: 1,
      name: 'Updated Template',
      site: 'Site1',
      version: 2,
      isRetired: false,
      rowVersion: 'v2',
    };

    service.updateTemplate(1, request).subscribe((result) => {
      expect(result).toEqual(mockTemplate);
    });

    const req = httpMock.expectOne('/analysis-templates/1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(mockTemplate);
  });

  it('should retire a template', () => {
    service.retireTemplate(1).subscribe(() => {
      expect(true).toBe(true);
    });

    const req = httpMock.expectOne('/analysis-templates/1/retire');
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });
});

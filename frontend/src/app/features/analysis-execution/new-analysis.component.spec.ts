import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { describe, it, beforeEach, expect, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { Router, provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { NewAnalysisComponent } from './new-analysis.component';
import { TemplatesApiService } from '../templates/services/templates-api.service';
import { AnalysisExecutionApiService, CreatedAnalysisDto } from './services/analysis-execution-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { AnalysisTemplateDto } from '../../shared/generated/models/analysis-template-dto';

describe('NewAnalysisComponent', () => {
  let component: NewAnalysisComponent;
  let fixture: ComponentFixture<NewAnalysisComponent>;
  let templatesApi: TemplatesApiService;
  let analysisApi: AnalysisExecutionApiService;
  let currentUser: CurrentUserService;
  let router: Router;

  const templates: AnalysisTemplateDto[] = [
    { id: 1, name: 'Pol Test', site: 'Inkerman', version: 1, isRetired: false, rowVersion: 'v1' },
    { id: 2, name: 'Brix Test', site: 'Invicta', version: 1, isRetired: false, rowVersion: 'v1' },
    { id: 3, name: 'Retired Test', site: 'Inkerman', version: 1, isRetired: true, rowVersion: 'v1' },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NewAnalysisComponent, HttpClientTestingModule],
      providers: [
        TemplatesApiService,
        AnalysisExecutionApiService,
        CurrentUserService,
        provideRouter([]),
        provideAnimationsAsync(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NewAnalysisComponent);
    component = fixture.componentInstance;
    templatesApi = TestBed.inject(TemplatesApiService);
    analysisApi = TestBed.inject(AnalysisExecutionApiService);
    currentUser = TestBed.inject(CurrentUserService);
    router = TestBed.inject(Router);
    currentUser.setUser({ sub: 'u1', username: 'u1', role: 'ControlLabAnalyst', site: 'Inkerman' });
  });

  it('should load templates and filter by site and retired status', () => {
    vi.spyOn(templatesApi, 'listTemplates').mockReturnValue(of(templates));

    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.templates()).toEqual([templates[0]]);
  });

  it('should show empty state when there are no active templates for the site', () => {
    vi.spyOn(templatesApi, 'listTemplates').mockReturnValue(of([templates[1], templates[2]]));

    fixture.detectChanges();

    expect(component.templates()).toEqual([]);
  });

  it('should handle error loading templates', () => {
    vi.spyOn(templatesApi, 'listTemplates').mockReturnValue(throwError(() => new Error('boom')));

    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.error()).toBeTruthy();
  });

  it('should create an analysis and navigate to it on success', () => {
    vi.spyOn(templatesApi, 'listTemplates').mockReturnValue(of(templates));
    const created: CreatedAnalysisDto = { analysisId: 42, sampleId: 7, sampleIdentifier: 'INK-2026-0007' };
    const createSpy = vi.spyOn(analysisApi, 'createAnalysis').mockReturnValue(of(created));
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture.detectChanges();
    component.form.patchValue({ analysisTemplateId: '1', sampleIdentifier: '' });

    component.submit();

    expect(createSpy).toHaveBeenCalledWith({ analysisTemplateId: 1, sampleIdentifier: null });
    expect(navigateSpy).toHaveBeenCalledWith(['/analysis/analysis', 42]);
    expect(component.submitting()).toBe(false);
  });

  it('should set submitError on a 400 validation failure', () => {
    vi.spyOn(templatesApi, 'listTemplates').mockReturnValue(of(templates));
    vi.spyOn(analysisApi, 'createAnalysis').mockReturnValue(
      throwError(() => ({ status: 400, error: { detail: 'Template has no active version' } }))
    );

    fixture.detectChanges();
    component.form.patchValue({ analysisTemplateId: '1', sampleIdentifier: '' });

    component.submit();

    expect(component.submitError()).toBe('Template has no active version');
    expect(component.submitting()).toBe(false);
  });

  it('should not submit an invalid form', () => {
    vi.spyOn(templatesApi, 'listTemplates').mockReturnValue(of(templates));
    const createSpy = vi.spyOn(analysisApi, 'createAnalysis');

    fixture.detectChanges();
    component.submit();

    expect(createSpy).not.toHaveBeenCalled();
  });
});

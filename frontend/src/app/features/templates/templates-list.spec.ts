import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { describe, it, beforeEach, expect, vi } from 'vitest';
import { TemplatesListComponent } from './templates-list.component';
import { TemplatesApiService } from './services/templates-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { of, throwError } from 'rxjs';
import { AnalysisTemplateDto } from '../../shared/generated/models/analysis-template-dto';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

describe('TemplatesListComponent', () => {
  let component: TemplatesListComponent;
  let fixture: ComponentFixture<TemplatesListComponent>;
  let apiService: TemplatesApiService;
  let currentUserService: CurrentUserService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TemplatesListComponent, HttpClientTestingModule],
      providers: [
        TemplatesApiService,
        CurrentUserService,
        provideRouter([]),
        provideAnimationsAsync(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TemplatesListComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(TemplatesApiService);
    currentUserService = TestBed.inject(CurrentUserService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load templates on init', () => {
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
    vi.spyOn(apiService, 'listTemplates').mockReturnValue(of(mockTemplates));

    fixture.detectChanges();

    expect(component.templates()).toEqual(mockTemplates);
    expect(component.loading()).toBe(false);
  });

  it('should handle error loading templates', () => {
    vi.spyOn(apiService, 'listTemplates').mockReturnValue(
      throwError(() => new Error('Failed to load'))
    );

    fixture.detectChanges();

    expect(component.loading()).toBe(false);
    expect(component.error()).toBeTruthy();
  });

  it('should show create button for lab coordinator', () => {
    currentUserService.setUser({
      sub: 'user1',
      username: 'user1',
      role: 'LabCoordinator',
      site: 'Site1',
    });
    vi.spyOn(apiService, 'listTemplates').mockReturnValue(of([]));

    fixture.detectChanges();

    expect(component.isLabCoordinator()).toBe(true);
  });

  it('should not show create button for analyst', () => {
    currentUserService.setUser({
      sub: 'user1',
      username: 'user1',
      role: 'ControlLabAnalyst',
      site: 'Site1',
    });
    vi.spyOn(apiService, 'listTemplates').mockReturnValue(of([]));

    fixture.detectChanges();

    expect(component.isLabCoordinator()).toBe(false);
  });

  it('should retire a template', () => {
    const mockTemplate: AnalysisTemplateDto = {
      id: 1,
      name: 'Template 1',
      site: 'Site1',
      version: 1,
      isRetired: false,
      rowVersion: 'v1',
    };
    currentUserService.setUser({
      sub: 'user1',
      username: 'user1',
      role: 'LabCoordinator',
      site: 'Site1',
    });
    vi.spyOn(apiService, 'listTemplates').mockReturnValue(of([mockTemplate]));
    const retireSpy = vi.spyOn(apiService, 'retireTemplate').mockReturnValue(of(void 0));
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    component.retire(mockTemplate);

    expect(retireSpy).toHaveBeenCalledWith(1);
  });

  it('should not retire if confirm is cancelled', () => {
    const mockTemplate: AnalysisTemplateDto = {
      id: 1,
      name: 'Template 1',
      site: 'Site1',
      version: 1,
      isRetired: false,
      rowVersion: 'v1',
    };
    const retireSpy = vi.spyOn(apiService, 'retireTemplate').mockReturnValue(of(void 0));
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    component.retire(mockTemplate);

    expect(retireSpy).not.toHaveBeenCalled();
  });
});

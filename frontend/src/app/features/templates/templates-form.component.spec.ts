import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { describe, it, beforeEach, expect, vi } from 'vitest';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { of } from 'rxjs';
import { TemplatesFormComponent } from './templates-form.component';
import { TemplatesApiService } from './services/templates-api.service';
import { AnalysisTemplateDto } from '../../shared/generated/models/analysis-template-dto';

describe('TemplatesFormComponent', () => {
  let component: TemplatesFormComponent;
  let fixture: ComponentFixture<TemplatesFormComponent>;
  let apiService: TemplatesApiService;

  function setup(templateId: string | null): void {
    TestBed.configureTestingModule({
      imports: [TemplatesFormComponent, HttpClientTestingModule],
      providers: [
        TemplatesApiService,
        provideRouter([]),
        provideAnimationsAsync(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: (key: string) => (key === 'id' ? templateId : null) } },
          },
        },
      ],
    });

    fixture = TestBed.createComponent(TemplatesFormComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(TemplatesApiService);
  }

  const mockTemplate: AnalysisTemplateDto = {
    id: 1,
    name: 'Cane Analysis',
    site: 'Inkerman',
    version: 1,
    isRetired: false,
    rowVersion: 'v1',
    testConfiguration: JSON.stringify({
      tests: [
        { id: 1, name: 'Pol', unit: '°Z', method: 'BSES' },
        { id: 2, name: 'Temperature', unit: '°C' },
      ],
      sampleMethod: 'Single (snap)',
    }),
  };

  describe('create mode', () => {
    beforeEach(() => setup(null));

    it('starts with zero test rows', () => {
      fixture.detectChanges();
      expect(component.testsArray.length).toBe(0);
      expect(component.rowKeys.length).toBe(0);
    });

    it('adds and removes test rows', () => {
      fixture.detectChanges();

      component.addTestRow();
      component.addTestRow();
      expect(component.testsArray.length).toBe(2);
      expect(component.rowKeys.length).toBe(2);

      const keyToRemove = component.rowKeys[0];
      component.removeTestRow(0, keyToRemove);

      expect(component.testsArray.length).toBe(1);
      expect(component.rowKeys).not.toContain(keyToRemove);
    });

    it('is invalid to submit with zero test rows', () => {
      fixture.detectChanges();
      component.form.patchValue({ name: 'New Template', site: 'Inkerman' });
      expect(component.form.invalid).toBe(true);
      expect(component.testsArray.invalid).toBe(true);
    });

    it('serializes new rows with assigned incrementing ids and no sampleMethod key when absent', () => {
      const createSpy = vi.spyOn(apiService, 'createTemplate').mockReturnValue(of(mockTemplate));
      fixture.detectChanges();

      component.form.patchValue({ name: 'New Template', site: 'Inkerman' });
      component.addTestRow();
      component.addTestRow();
      component.testsArray.at(0).patchValue({ name: 'Pol', unit: '°Z', method: 'BSES' });
      component.testsArray.at(1).patchValue({ name: 'Brix', unit: '°Bx' });

      component.submit();

      expect(createSpy).toHaveBeenCalledTimes(1);
      const request = createSpy.mock.calls[0][0];
      const config = JSON.parse(request.testConfiguration!);
      expect(config.tests).toEqual([
        { id: 1, name: 'Pol', unit: '°Z', method: 'BSES' },
        { id: 2, name: 'Brix', unit: '°Bx' },
      ]);
      expect(config.sampleMethod).toBeUndefined();
    });
  });

  describe('edit mode', () => {
    beforeEach(() => setup('1'));

    it('parses the existing config into rows and preserves sampleMethod', () => {
      vi.spyOn(apiService, 'getTemplate').mockReturnValue(of(mockTemplate));

      fixture.detectChanges();

      expect(component.testsArray.length).toBe(2);
      expect(component.testsArray.at(0).value).toEqual({ id: 1, name: 'Pol', unit: '°Z', method: 'BSES' });
      expect(component.testsArray.at(1).value).toEqual({ id: 2, name: 'Temperature', unit: '°C', method: '' });
      expect(component.availableUnits).toContain('°Z');
    });

    it('keeps stable ids for existing rows and preserves sampleMethod on save', () => {
      vi.spyOn(apiService, 'getTemplate').mockReturnValue(of(mockTemplate));
      const updateSpy = vi.spyOn(apiService, 'updateTemplate').mockReturnValue(of(mockTemplate));

      fixture.detectChanges();
      component.addTestRow();
      component.testsArray.at(2).patchValue({ name: 'Ash', unit: '%' });

      component.submit();

      expect(updateSpy).toHaveBeenCalledTimes(1);
      const request = updateSpy.mock.calls[0][1];
      const config = JSON.parse(request.testConfiguration!);
      expect(config.sampleMethod).toBe('Single (snap)');
      expect(config.tests).toEqual([
        { id: 1, name: 'Pol', unit: '°Z', method: 'BSES' },
        { id: 2, name: 'Temperature', unit: '°C' },
        { id: 3, name: 'Ash', unit: '%' },
      ]);
    });

    it('starts with zero rows and empty preserved config when testConfiguration is malformed', () => {
      vi.spyOn(apiService, 'getTemplate').mockReturnValue(
        of({ ...mockTemplate, testConfiguration: '{not json' })
      );

      fixture.detectChanges();

      expect(component.testsArray.length).toBe(0);
      expect(component.rowKeys.length).toBe(0);
    });
  });
});

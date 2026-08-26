import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of } from 'rxjs';
import axe from 'axe-core';
import { CalibrationCurvesComponent } from './calibration-curves.component';
import {
  CalibrationCurveDto,
  CalibrationCurvesApiService,
} from './services/calibration-curves-api.service';

describe('CalibrationCurvesComponent - Accessibility', () => {
  let fixture: ComponentFixture<CalibrationCurvesComponent>;

  const mockCurve: CalibrationCurveDto = {
    id: 1,
    name: 'Brix Standard Curve',
    analysisTemplateId: 50,
    templateName: 'Final Molasses Purity',
    site: 'Invicta',
    isActive: true,
    points: [
      { xValue: 0, yValue: 0.1 },
      { xValue: 5, yValue: 5.2 },
      { xValue: 10, yValue: 9.8 },
    ],
    rowVersion: 'v1.0',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CalibrationCurvesComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(CalibrationCurvesComponent);
    const apiService = TestBed.inject(CalibrationCurvesApiService);
    vi.spyOn(apiService, 'listCurves').mockReturnValue(of([mockCurve]));
    fixture.detectChanges();
  });

  it('should not have any a11y violations', async () => {
    const results = await axe.run(fixture.nativeElement, {
      rules: {
        'color-contrast': { enabled: false },
      },
    });

    expect(results.violations).toEqual([]);
  });
});

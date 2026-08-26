import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError, NEVER } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { CalibrationCurvesComponent } from './calibration-curves.component';
import {
  CalibrationCurveDto,
  CalibrationCurvesApiService,
} from './services/calibration-curves-api.service';

describe('CalibrationCurvesComponent', () => {
  let component: CalibrationCurvesComponent;
  let fixture: ComponentFixture<CalibrationCurvesComponent>;
  let apiService: CalibrationCurvesApiService;

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

  const mockCurve2: CalibrationCurveDto = {
    ...mockCurve,
    id: 2,
    name: 'pH Standard Curve',
    isActive: false,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CalibrationCurvesComponent],
      providers: [CalibrationCurvesApiService, provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(CalibrationCurvesComponent);
    component = fixture.componentInstance;
    apiService = TestBed.inject(CalibrationCurvesApiService);
  });

  it('should show loading state while fetching', () => {
    // Never emit so the loading state persists through the first detectChanges (which runs ngOnInit).
    vi.spyOn(apiService, 'listCurves').mockReturnValue(NEVER);

    fixture.detectChanges();

    expect(component.loading()).toBe(true);
    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeTruthy();
  });

  it('should load curves and default-select the first one on init', () => {
    vi.spyOn(apiService, 'listCurves').mockReturnValue(of([mockCurve, mockCurve2]));

    component.ngOnInit();

    expect(component.curves()).toEqual([mockCurve, mockCurve2]);
    expect(component.loading()).toBe(false);
    expect(component.selectedCurve()?.id).toBe(1);
  });

  it('should render forbidden state on 403', () => {
    vi.spyOn(apiService, 'listCurves').mockReturnValue(
      throwError(() => ({ status: 403 })),
    );

    component.ngOnInit();
    fixture.detectChanges();

    expect(component.forbidden()).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Coordinator access required');
  });

  it('should render error state on non-403 failure', () => {
    vi.spyOn(apiService, 'listCurves').mockReturnValue(
      throwError(() => ({ status: 500 })),
    );

    component.ngOnInit();
    fixture.detectChanges();

    expect(component.error()).toContain('Failed to load');
    expect(component.forbidden()).toBe(false);
  });

  it('should render empty state when no curves', () => {
    vi.spyOn(apiService, 'listCurves').mockReturnValue(of([]));

    component.ngOnInit();
    fixture.detectChanges();

    expect(component.curves()).toEqual([]);
    expect(fixture.nativeElement.textContent).toContain('No calibration curves');
  });

  it('should render a row per curve with name, template, site and status', () => {
    vi.spyOn(apiService, 'listCurves').mockReturnValue(of([mockCurve, mockCurve2]));

    component.ngOnInit();
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Brix Standard Curve');
    expect(rows[0].textContent).toContain('Final Molasses Purity');
    expect(rows[0].textContent).toContain('Invicta');
    expect(rows[0].textContent).toContain('Active');
  });

  it('should show the points count in the detail panel', () => {
    vi.spyOn(apiService, 'listCurves').mockReturnValue(of([mockCurve, mockCurve2]));

    component.ngOnInit();
    fixture.detectChanges();

    // mockCurve has 3 points; the selected-curve panel reports the count.
    expect(fixture.nativeElement.textContent).toContain('3 points');
  });

  it('should update selectedCurve when a different row is selected', () => {
    vi.spyOn(apiService, 'listCurves').mockReturnValue(of([mockCurve, mockCurve2]));

    component.ngOnInit();
    component.selectCurve(mockCurve2);

    expect(component.selectedCurve()?.id).toBe(2);
  });

  it('should reload curves on retry', () => {
    vi.spyOn(apiService, 'listCurves').mockReturnValue(of([mockCurve]));

    component.reload();

    expect(component.curves()).toEqual([mockCurve]);
  });
});

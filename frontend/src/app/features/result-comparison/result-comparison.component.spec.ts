import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ResultComparisonComponent } from './result-comparison.component';

describe('ResultComparisonComponent', () => {
  let component: ResultComparisonComponent;
  let fixture: ComponentFixture<ResultComparisonComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResultComparisonComponent, HttpClientTestingModule],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(ResultComparisonComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create and auto-load on init', () => {
    fixture.detectChanges();

    expect(component.loading()).toBe(true);

    httpMock.expectOne((r) => r.url.includes('/analysis-templates')).flush([]);
    httpMock.expectOne((r) => r.url.includes('/results/comparison')).flush({
      unit: null,
      toleranceMin: null,
      toleranceMax: null,
      totalPoints: 0,
      points: [],
    });

    fixture.detectChanges();
    expect(component.loading()).toBe(false);
  });

  it('renders chart and table for a payload with multiple samples', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url.includes('/analysis-templates')).flush([]);

    httpMock.expectOne((r) => r.url.includes('/results/comparison')).flush({
      unit: 'mg/L',
      toleranceMin: 10,
      toleranceMax: 20,
      totalPoints: 2,
      points: [
        {
          analysisId: 1,
          sampleId: 1,
          sampleIdentifier: 'Sample001',
          templateName: 'Template1',
          testId: 1,
          value: 15,
          unit: 'mg/L',
          capturedAtUtc: '2026-08-26T10:00:00Z',
          validationResult: 'Valid',
          calibratedValue: null,
        },
        {
          analysisId: 2,
          sampleId: 2,
          sampleIdentifier: 'Sample002',
          templateName: 'Template1',
          testId: 1,
          value: 25,
          unit: 'mg/L',
          capturedAtUtc: '2026-08-26T11:00:00Z',
          validationResult: 'OutOfTolerance',
          calibratedValue: null,
        },
      ],
    });

    fixture.detectChanges();

    expect(component.points().length).toBe(2);
    expect(component.distinctSampleCount()).toBe(2);
    expect(component.outOfSpecCount()).toBe(1);
    expect(component.chartSeries().length).toBe(2);

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelectorAll('tbody tr').length).toBe(2);
    expect(compiled.textContent).toContain('Sample001');
    expect(compiled.textContent).toContain('Out of spec');
  });

  it('folds samples beyond the top 5 into an "Other" series', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url.includes('/analysis-templates')).flush([]);

    const points = Array.from({ length: 6 }, (_, i) => ({
      analysisId: i + 1,
      sampleId: i + 1,
      sampleIdentifier: `Sample00${i + 1}`,
      templateName: 'Template1',
      testId: 1,
      value: 10 + i,
      unit: 'mg/L',
      capturedAtUtc: `2026-08-26T1${i}:00:00Z`,
      validationResult: 'Valid',
      calibratedValue: null,
    }));

    httpMock.expectOne((r) => r.url.includes('/results/comparison')).flush({
      unit: 'mg/L',
      toleranceMin: null,
      toleranceMax: null,
      totalPoints: 6,
      points,
    });

    fixture.detectChanges();

    expect(component.chartSeries().length).toBe(6);
    const otherSeries = component.chartSeries().find((s) => s.isOther);
    expect(otherSeries).toBeTruthy();
    expect(otherSeries?.points.length).toBe(1);
  });

  it('shows the empty state when there are no points', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url.includes('/analysis-templates')).flush([]);
    httpMock.expectOne((r) => r.url.includes('/results/comparison')).flush({
      unit: null,
      toleranceMin: null,
      toleranceMax: null,
      totalPoints: 0,
      points: [],
    });

    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.textContent).toContain('No results to compare');
  });

  it('shows an error card with retry on failure', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url.includes('/analysis-templates')).flush([]);
    httpMock.expectOne((r) => r.url.includes('/results/comparison')).error(new ProgressEvent('error'));

    fixture.detectChanges();

    expect(component.error()).toContain('Failed to load');
    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.textContent).toContain('Retry');
  });
});

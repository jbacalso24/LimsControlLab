import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ResultComparisonChartComponent, ResultComparisonChartSeries } from './result-comparison-chart.component';
import { ResultComparisonPoint } from './services/result-comparison.models';

function point(overrides: Partial<ResultComparisonPoint>): ResultComparisonPoint {
  return {
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
    ...overrides,
  };
}

describe('ResultComparisonChartComponent', () => {
  let component: ResultComparisonChartComponent;
  let fixture: ComponentFixture<ResultComparisonChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ResultComparisonChartComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ResultComparisonChartComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('renders one polyline and circle marker per series/point', () => {
    const series: ResultComparisonChartSeries[] = [
      {
        key: 'Sample001',
        label: 'Sample001',
        colorIndex: 0,
        points: [point({}), point({ analysisId: 2, capturedAtUtc: '2026-08-26T11:00:00Z', value: 18 })],
      },
      {
        key: 'Sample002',
        label: 'Sample002',
        colorIndex: 1,
        points: [point({ analysisId: 3, sampleIdentifier: 'Sample002', value: 30, validationResult: 'OutOfTolerance' })],
      },
    ];

    fixture.componentRef.setInput('series', series);
    fixture.detectChanges();

    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelectorAll('polyline').length).toBe(2);
    // 3 points total: 2 in-spec circles + 1 out-of-spec ring (2 circles) = 4 circles
    expect(compiled.querySelectorAll('circle').length).toBe(4);
    expect(component.showLegend()).toBe(true);
  });

  it('does not render a legend for a single series', () => {
    fixture.componentRef.setInput('series', [
      { key: 'Sample001', label: 'Sample001', colorIndex: 0, points: [point({})] },
    ]);
    fixture.detectChanges();

    expect(component.showLegend()).toBe(false);
    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.querySelector('[aria-label="Sample series legend"]')).toBeNull();
  });

  it('renders the tolerance band only when both bounds are provided', () => {
    fixture.componentRef.setInput('series', [
      { key: 'Sample001', label: 'Sample001', colorIndex: 0, points: [point({})] },
    ]);
    fixture.componentRef.setInput('toleranceMin', 10);
    fixture.componentRef.setInput('toleranceMax', 20);
    fixture.detectChanges();

    expect(component.toleranceBand()).not.toBeNull();

    fixture.componentRef.setInput('toleranceMax', null);
    fixture.detectChanges();

    expect(component.toleranceBand()).toBeNull();
  });

  it('shows the fallback message when there are no points', () => {
    fixture.componentRef.setInput('series', []);
    fixture.detectChanges();

    expect(component.hasPoints()).toBe(false);
    const compiled: HTMLElement = fixture.nativeElement;
    expect(compiled.textContent).toContain('No points to plot');
  });
});

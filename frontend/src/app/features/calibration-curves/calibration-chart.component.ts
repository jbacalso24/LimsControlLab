import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  signal,
} from '@angular/core';
import { CalibrationPointDto } from './services/calibration-curves-api.service';

interface PlottedPoint {
  x: number;
  y: number;
  screenX: number;
  screenY: number;
}

/** Fixed SVG canvas size (viewBox units); scales responsively via width:100%. */
const WIDTH = 400;
const HEIGHT = 240;
const MARGIN = { top: 16, right: 16, bottom: 32, left: 44 };

/**
 * Dependency-free inline-SVG line chart for a single calibration curve.
 * Plots measured points against a faint y=x reference line so a coordinator
 * can eyeball how far the curve drifts from ideal calibration.
 */
@Component({
  selector: 'lims-calibration-chart',
  standalone: true,
  templateUrl: './calibration-chart.component.html',
  styleUrl: './calibration-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CalibrationChartComponent {
  readonly points = input<CalibrationPointDto[]>([]);
  readonly name = input<string>('');
  readonly xUnit = input<string>('');
  readonly yUnit = input<string>('');

  readonly hoveredIndex = signal<number | null>(null);
  readonly ready = signal(false);

  readonly width = WIDTH;
  readonly height = HEIGHT;

  private readonly numericPoints = computed(() =>
    this.points()
      .map((p) => ({ x: Number(p.xValue), y: Number(p.yValue) }))
      .sort((a, b) => a.x - b.x),
  );

  readonly hasEnoughPoints = computed(() => this.numericPoints().length >= 2);

  private readonly xDomain = computed(() => this.domainFor(this.numericPoints().map((p) => p.x)));
  private readonly yDomain = computed(() => this.domainFor(this.numericPoints().map((p) => p.y)));

  readonly xAxisLabel = computed(() => (this.xUnit() ? `Input (X) (${this.xUnit()})` : 'Input (X)'));
  readonly yAxisLabel = computed(() =>
    this.yUnit() ? `Calibrated (Y) (${this.yUnit()})` : 'Calibrated (Y)',
  );

  readonly plottedPoints = computed<PlottedPoint[]>(() =>
    this.numericPoints().map((p) => ({
      x: p.x,
      y: p.y,
      screenX: this.scaleX(p.x),
      screenY: this.scaleY(p.y),
    })),
  );

  readonly polylinePoints = computed(() =>
    this.plottedPoints()
      .map((p) => `${p.screenX},${p.screenY}`)
      .join(' '),
  );

  readonly referenceLine = computed(() => {
    const shared = this.domainFor([
      this.xDomain().min,
      this.xDomain().max,
      this.yDomain().min,
      this.yDomain().max,
    ]);
    return {
      x1: this.scaleX(shared.min),
      y1: this.scaleY(shared.min),
      x2: this.scaleX(shared.max),
      y2: this.scaleY(shared.max),
    };
  });

  readonly xTicks = computed(() => this.ticksFor(this.xDomain(), (v) => this.scaleX(v)));
  readonly yTicks = computed(() => this.ticksFor(this.yDomain(), (v) => this.scaleY(v)));

  readonly plotLeft = MARGIN.left;
  readonly plotRight = WIDTH - MARGIN.right;
  readonly plotTop = MARGIN.top;
  readonly plotBottom = HEIGHT - MARGIN.bottom;

  constructor() {
    // Gentle fade-in each time the plotted curve changes (e.g. a new row is selected).
    // Reduced-motion users get the final state immediately via the CSS media query.
    effect(() => {
      this.numericPoints();
      this.ready.set(false);
      queueMicrotask(() => this.ready.set(true));
    });
  }

  hover(index: number): void {
    this.hoveredIndex.set(index);
  }

  clearHover(): void {
    this.hoveredIndex.set(null);
  }

  formatTick(value: number): string {
    return Number(value.toFixed(2)).toString();
  }

  private domainFor(values: number[]): { min: number; max: number } {
    if (!values.length) return { min: 0, max: 1 };
    const min = Math.min(...values);
    const max = Math.max(...values);
    const range = max - min || 1;
    const pad = range * 0.1;
    return { min: min - pad, max: max + pad };
  }

  private ticksFor(domain: { min: number; max: number }, scale: (v: number) => number) {
    const count = 4;
    const step = (domain.max - domain.min) / count;
    return Array.from({ length: count + 1 }, (_, i) => {
      const value = domain.min + step * i;
      return { value, screen: scale(value) };
    });
  }

  private scaleX(x: number): number {
    const { min, max } = this.xDomain();
    const ratio = (x - min) / (max - min || 1);
    return this.plotLeft + ratio * (this.plotRight - this.plotLeft);
  }

  private scaleY(y: number): number {
    const { min, max } = this.yDomain();
    const ratio = (y - min) / (max - min || 1);
    return this.plotBottom - ratio * (this.plotBottom - this.plotTop);
  }
}

import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  signal,
} from '@angular/core';
import { ResultComparisonPoint } from './services/result-comparison.models';

/** A pass/pass-like validation result. Anything else (incl. null/unknown) is treated as out-of-spec. */
const PASS_VALUES = new Set(['Valid', 'Pass', 'InSpec']);

export interface ResultComparisonChartSeries {
  /** Grouping key, usually the sample identifier ("Other" for the folded overflow bucket). */
  key: string;
  label: string;
  /** Fixed-order palette index (0-4 -> --chart-1..5). Ignored when isOther is true. */
  colorIndex: number;
  /** Folds low-rank samples into one muted series instead of inventing a 6th hue. */
  isOther?: boolean;
  points: ResultComparisonPoint[];
}

interface PlottedPoint {
  x: number;
  y: number;
  screenX: number;
  screenY: number;
  outOfSpec: boolean;
  raw: ResultComparisonPoint;
}

interface PlottedSeries {
  key: string;
  label: string;
  colorVar: string;
  polylinePoints: string;
  points: PlottedPoint[];
}

/** Fixed SVG canvas size (viewBox units); scales responsively via width:100%. */
const WIDTH = 480;
const HEIGHT = 280;
const MARGIN = { top: 16, right: 16, bottom: 40, left: 48 };

/**
 * Dependency-free inline-SVG time-vs-value scatter for comparing readings across samples.
 * One series per sample, connected by a line in capture order; a faint tolerance band
 * shows in-spec range; out-of-spec points get a hollow-ring marker (not color alone).
 */
@Component({
  selector: 'lims-result-comparison-chart',
  standalone: true,
  templateUrl: './result-comparison-chart.component.html',
  styleUrl: './result-comparison-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultComparisonChartComponent {
  readonly series = input<ResultComparisonChartSeries[]>([]);
  readonly toleranceMin = input<number | null>(null);
  readonly toleranceMax = input<number | null>(null);
  readonly unit = input<string>('');

  readonly hoveredKey = signal<string | null>(null);
  readonly ready = signal(false);

  readonly width = WIDTH;
  readonly height = HEIGHT;

  readonly showLegend = computed(() => this.series().length >= 2);

  private static isPass(validationResult: string | null): boolean {
    return !!validationResult && PASS_VALUES.has(validationResult);
  }

  private readonly numericSeries = computed(() =>
    this.series().map((s) => ({
      key: s.key,
      label: s.label,
      colorVar: s.isOther ? 'var(--muted-foreground)' : `var(--chart-${s.colorIndex + 1})`,
      points: [...s.points]
        .map((p) => ({
          x: new Date(p.capturedAtUtc).getTime(),
          y: Number(p.value),
          outOfSpec: !ResultComparisonChartComponent.isPass(p.validationResult),
          raw: p,
        }))
        .sort((a, b) => a.x - b.x),
    })),
  );

  private readonly allPoints = computed(() => this.numericSeries().flatMap((s) => s.points));

  readonly hasPoints = computed(() => this.allPoints().length > 0);

  private readonly xDomain = computed(() => this.domainFor(this.allPoints().map((p) => p.x)));
  private readonly yDomain = computed(() => {
    const values = this.allPoints().map((p) => p.y);
    const min = this.toleranceMin();
    const max = this.toleranceMax();
    if (min !== null) values.push(min);
    if (max !== null) values.push(max);
    return this.domainFor(values);
  });

  readonly yAxisLabel = computed(() => (this.unit() ? `Value (${this.unit()})` : 'Value'));

  readonly plotLeft = MARGIN.left;
  readonly plotRight = WIDTH - MARGIN.right;
  readonly plotTop = MARGIN.top;
  readonly plotBottom = HEIGHT - MARGIN.bottom;

  readonly xTicks = computed(() => this.ticksFor(this.xDomain(), (v) => this.scaleX(v)));
  readonly yTicks = computed(() => this.ticksFor(this.yDomain(), (v) => this.scaleY(v)));

  readonly toleranceBand = computed(() => {
    const min = this.toleranceMin();
    const max = this.toleranceMax();
    if (min === null || max === null) return null;
    return {
      y: this.scaleY(max),
      height: Math.max(this.scaleY(min) - this.scaleY(max), 0),
    };
  });

  readonly plottedSeries = computed<PlottedSeries[]>(() =>
    this.numericSeries().map((s) => {
      const points = s.points.map((p) => ({
        x: p.x,
        y: p.y,
        outOfSpec: p.outOfSpec,
        raw: p.raw,
        screenX: this.scaleX(p.x),
        screenY: this.scaleY(p.y),
      }));
      return {
        key: s.key,
        label: s.label,
        colorVar: s.colorVar,
        points,
        polylinePoints: points.map((p) => `${p.screenX},${p.screenY}`).join(' '),
      };
    }),
  );

  constructor() {
    // Gentle fade-in whenever the plotted series change (e.g. a new filter is applied).
    // Reduced-motion users get the final state immediately via the CSS media query.
    effect(() => {
      this.numericSeries();
      this.ready.set(false);
      queueMicrotask(() => this.ready.set(true));
    });
  }

  hover(key: string): void {
    this.hoveredKey.set(key);
  }

  clearHover(): void {
    this.hoveredKey.set(null);
  }

  formatTick(value: number): string {
    return Number(value.toFixed(2)).toString();
  }

  formatTimeTick(value: number): string {
    return new Intl.DateTimeFormat('en-US', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  formatTimeFull(value: string): string {
    return new Intl.DateTimeFormat('en-US', {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
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

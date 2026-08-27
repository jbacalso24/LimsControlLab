import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ResultComparisonApiService } from './services/result-comparison-api.service';
import { ResultComparisonPoint, ResultComparisonResponse } from './services/result-comparison.models';
import { ResultComparisonChartComponent, ResultComparisonChartSeries } from './result-comparison-chart.component';
import { TemplatesApiService } from '../templates/services/templates-api.service';
import { AnalysisTemplateDto } from '../../shared/generated/models/analysis-template-dto';
import { TestDefinitionDto } from '../../shared/generated/models/test-definition-dto';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardInputComponent } from '@/shared/components/input/input.component';
import { ZardSelectComponent, ZardSelectItemComponent } from '@/shared/components/select';
import { ZardCardComponent, ZardCardHeaderComponent, ZardCardTitleComponent, ZardCardDescriptionComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardSkeletonComponent } from '@/shared/components/skeleton/skeleton.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import { ZardTableImports } from '@/shared/components/table/table.imports';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideX, lucideSearch, lucideRefreshCw, lucideInbox } from '@ng-icons/lucide';

/** A pass/pass-like validation result. Anything else (incl. null/unknown) is treated as out-of-spec. */
const PASS_VALUES = new Set(['Valid', 'Pass', 'InSpec']);
function isPass(validationResult: string | null): boolean {
  return !!validationResult && PASS_VALUES.has(validationResult);
}

/** Parses a template's `testConfiguration` JSON (`{ tests: [{id,name,unit,method?}], ... }`) into its test list. */
function parseTemplateTests(raw: string | null | undefined): TestDefinitionDto[] {
  if (!raw) return [];
  try {
    const parsed: unknown = JSON.parse(raw);
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return [];
    const tests = (parsed as Record<string, unknown>)['tests'];
    if (!Array.isArray(tests)) return [];
    return tests
      .filter((t): t is Record<string, unknown> => !!t && typeof t === 'object')
      .map((t) => ({
        id: (t['id'] as number | string) ?? '',
        name: typeof t['name'] === 'string' ? (t['name'] as string) : '',
        unit: typeof t['unit'] === 'string' ? (t['unit'] as string) : '',
        method: typeof t['method'] === 'string' ? (t['method'] as string) : undefined,
      }));
  } catch {
    return [];
  }
}

@Component({
  selector: 'lims-result-comparison',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardButtonComponent,
    ZardInputComponent,
    ZardSelectComponent,
    ZardSelectItemComponent,
    ZardCardComponent,
    ZardCardHeaderComponent,
    ZardCardTitleComponent,
    ZardCardDescriptionComponent,
    ZardCardContentComponent,
    ZardAlertComponent,
    ZardSkeletonComponent,
    ZardEmptyComponent,
    ...ZardTableImports,
    ResultComparisonChartComponent,
    NgIcon,
  ],
  viewProviders: [provideIcons({ lucideX, lucideSearch, lucideRefreshCw, lucideInbox })],
  templateUrl: './result-comparison.component.html',
  styleUrl: './result-comparison.component.scss',
})
export class ResultComparisonComponent {
  private apiService = inject(ResultComparisonApiService);
  private templatesApi = inject(TemplatesApiService);
  private fb = inject(FormBuilder);
  private datePipe = new DatePipe('en-US');

  filterForm: FormGroup = this.fb.group({
    templateName: [''],
    testId: [''],
    sampleIdentifier: [''],
    fromUtc: [''],
    toUtc: [''],
  });

  loading = signal(false);
  error = signal('');
  searched = signal(false);
  response = signal<ResultComparisonResponse | null>(null);
  templates = signal<AnalysisTemplateDto[]>([]);

  /** Mirrors the templateName/testId controls so template options can drive computed()s. */
  private templateNameValue = signal('');
  private testIdValue = signal('');

  templateOptions = computed(() => {
    const seen = new Set<string>();
    return this.templates()
      .filter((t) => !t.isRetired)
      .filter((t) => (seen.has(t.name) ? false : (seen.add(t.name), true)))
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  testOptions = computed<TestDefinitionDto[]>(() => {
    const template = this.templates().find((t) => t.name === this.templateNameValue());
    return parseTemplateTests(template?.testConfiguration);
  });

  selectedTestUnit = computed(
    () => this.testOptions().find((t) => String(t.id) === this.testIdValue())?.unit ?? '',
  );

  points = computed<ResultComparisonPoint[]>(() => this.response()?.points ?? []);
  tableRows = computed(() =>
    [...this.points()].sort((a, b) => a.capturedAtUtc.localeCompare(b.capturedAtUtc)),
  );

  private mean = computed(() => {
    const values = this.points().map((p) => p.value);
    return values.length ? values.reduce((sum, v) => sum + v, 0) / values.length : 0;
  });

  distinctSampleCount = computed(() => new Set(this.points().map((p) => p.sampleIdentifier)).size);
  foldedSampleCount = computed(() => Math.max(0, this.distinctSampleCount() - 5));
  outOfSpecCount = computed(() => this.points().filter((p) => !isPass(p.validationResult)).length);

  chartSeries = computed<ResultComparisonChartSeries[]>(() => {
    const points = this.points();
    if (!points.length) return [];
    const groups = new Map<string, ResultComparisonPoint[]>();
    for (const p of points) {
      const arr = groups.get(p.sampleIdentifier);
      if (arr) arr.push(p);
      else groups.set(p.sampleIdentifier, [p]);
    }
    const ranked = [...groups.entries()].sort(
      (a, b) => b[1].length - a[1].length || a[0].localeCompare(b[0]),
    );
    const top = ranked.slice(0, 5);
    const rest = ranked.slice(5);
    const series: ResultComparisonChartSeries[] = top.map(([key, pts], i) => ({
      key,
      label: key,
      colorIndex: i,
      points: pts,
    }));
    if (rest.length) {
      series.push({
        key: '__other__',
        label: `Other (+${rest.length} more sample${rest.length > 1 ? 's' : ''})`,
        colorIndex: -1,
        isOther: true,
        points: rest.flatMap(([, pts]) => pts),
      });
    }
    return series;
  });

  constructor() {
    this.filterForm.get('templateName')!.valueChanges.subscribe((v: string) => {
      this.templateNameValue.set(v ?? '');
      // Changing template invalidates the previously selected test.
      this.filterForm.get('testId')!.setValue('', { emitEvent: false });
      this.testIdValue.set('');
    });
    this.filterForm.get('testId')!.valueChanges.subscribe((v: string) => this.testIdValue.set(v ?? ''));
  }

  ngOnInit(): void {
    this.templatesApi.listTemplates().subscribe({
      next: (data) => this.templates.set(data),
      error: () => {
        // Template dropdown stays empty; the comparison search itself does not depend on it.
      },
    });
    // Auto-load with empty filters, matching History Search.
    this.search();
  }

  search(): void {
    this.loading.set(true);
    this.error.set('');
    this.searched.set(true);

    this.apiService.compare(this.buildRequest()).subscribe({
      next: (data) => {
        this.response.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load result comparison. Please try again.');
      },
    });
  }

  clearFilters(): void {
    this.filterForm.reset();
    this.search();
  }

  deltaFromMean(value: number): number {
    return value - this.mean();
  }

  isPass(validationResult: string | null): boolean {
    return isPass(validationResult);
  }

  formatCapturedAt(value: string): string | null {
    return this.datePipe.transform(value, 'short');
  }

  private buildRequest() {
    const formValue = this.filterForm.value;
    const request: { templateName?: string; testId?: number; sampleIdentifier?: string; fromUtc?: string; toUtc?: string } = {};

    if (formValue.templateName) request.templateName = formValue.templateName;
    if (formValue.testId) request.testId = Number(formValue.testId);
    if (formValue.sampleIdentifier) request.sampleIdentifier = formValue.sampleIdentifier;
    if (formValue.fromUtc) request.fromUtc = formValue.fromUtc;
    if (formValue.toUtc) request.toUtc = formValue.toUtc;

    return request;
  }
}

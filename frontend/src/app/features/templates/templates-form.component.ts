import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  AbstractControl,
  FormArray,
  FormBuilder,
  FormGroup,
  ValidationErrors,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardInputComponent } from '@/shared/components/input/input.component';
import { ZardSelectComponent, ZardSelectItemComponent } from '@/shared/components/select';
import { ZardJsonEditorComponent } from '@/shared/components/json-editor/json-editor.component';
import { ZardCardComponent, ZardCardHeaderComponent, ZardCardTitleComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { ToastService } from '@/shared/services/toast/toast.service';
import { BreadcrumbService } from '@/shared/services/breadcrumb/breadcrumb.service';
import { TemplatesApiService } from './services/templates-api.service';
import { CreateAnalysisTemplateRequest } from '../../shared/generated/models/create-analysis-template-request';
import { UpdateAnalysisTemplateRequest } from '../../shared/generated/models/update-analysis-template-request';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideTrash2, lucideX, lucideCheck, lucideAlertCircle } from '@ng-icons/lucide';

const SITES = ['Inkerman', 'Invicta', 'Kalamia', 'Victoria', 'Macknade', 'Proserpine', 'PlaneCreek', 'Pioneer'];
const CURATED_UNITS = ['°Z', '°C', '°Bx', '%', 'ICUMS', 'g/L', 'pH', 'mL', 'ppm'];

/** A single row of the structured test editor. `id` is null for rows not yet persisted. */
interface TestRowValue {
  id: number | null;
  name: string;
  unit: string;
  method: string;
}

function atLeastOneTest(control: AbstractControl): ValidationErrors | null {
  return control instanceof FormArray && control.length > 0 ? null : { required: true };
}

@Component({
  selector: 'lims-templates-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardButtonComponent,
    ZardInputComponent,
    ZardSelectComponent,
    ZardSelectItemComponent,
    ZardJsonEditorComponent,
    ZardCardComponent,
    ZardCardHeaderComponent,
    ZardCardTitleComponent,
    ZardCardContentComponent,
    ZardAlertComponent,
    ZardSpinnerComponent,
    NgIcon,
  ],
  templateUrl: './templates-form.component.html',
  styleUrl: './templates-form.component.scss',
  viewProviders: [provideIcons({ lucidePlus, lucideTrash2, lucideX, lucideCheck, lucideAlertCircle })],
})
export class TemplatesFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private apiService = inject(TemplatesApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);
  private breadcrumb = inject(BreadcrumbService);

  sites = SITES;
  form: FormGroup;
  loading = signal(false);
  submitting = signal(false);
  error = signal('');
  submitError = signal('');
  isEdit = signal(false);
  private templateId: number | null = null;
  private rowVersion = '';

  /** Every Test Configuration JSON key other than `tests`, preserved verbatim on save. */
  private preservedConfig: Record<string, unknown> = {};
  /** Stable per-row identity for @for tracking and exit animations (not the persisted test id). */
  rowKeys: number[] = [];
  private nextRowKey = 0;
  availableUnits: string[] = [...CURATED_UNITS];

  constructor() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      site: ['', Validators.required],
      minTolerance: [''],
      maxTolerance: [''],
      tests: this.fb.array([], atLeastOneTest),
      validationRules: [''],
      calculationDefinitions: [''],
    });
  }

  get testsArray(): FormArray {
    return this.form.get('tests') as FormArray;
  }

  testGroup(index: number): FormGroup {
    return this.testsArray.at(index) as FormGroup;
  }

  private buildTestRow(row?: Partial<TestRowValue>): FormGroup {
    return this.fb.group({
      id: [row?.id ?? null],
      name: [row?.name ?? '', Validators.required],
      unit: [row?.unit ?? '', Validators.required],
      method: [row?.method ?? ''],
    });
  }

  addTestRow(): void {
    this.testsArray.push(this.buildTestRow());
    this.rowKeys.push(this.nextRowKey++);
  }

  removeTestRow(index: number, key: number): void {
    const i = this.rowKeys.indexOf(key);
    if (i === -1) {
      return;
    }
    this.testsArray.removeAt(i);
    this.rowKeys.splice(i, 1);
  }

  /** Parses the Test Configuration JSON into rows + preserved keys. Never throws. */
  private parseTestConfiguration(raw: string | null | undefined): { rows: TestRowValue[]; preserved: Record<string, unknown> } {
    if (!raw) {
      return { rows: [], preserved: {} };
    }
    try {
      const parsed: unknown = JSON.parse(raw);
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
        return { rows: [], preserved: {} };
      }
      const { tests, ...preserved } = parsed as Record<string, unknown>;
      const rawTests = Array.isArray(tests) ? tests : [];
      const rows: TestRowValue[] = rawTests
        .filter((t): t is Record<string, unknown> => !!t && typeof t === 'object')
        .map((t) => ({
          id: typeof t['id'] === 'number' ? (t['id'] as number) : null,
          name: typeof t['name'] === 'string' ? (t['name'] as string) : '',
          unit: typeof t['unit'] === 'string' ? (t['unit'] as string) : '',
          method: typeof t['method'] === 'string' ? (t['method'] as string) : '',
        }));
      return { rows, preserved };
    } catch {
      return { rows: [], preserved: {} };
    }
  }

  private applyTestConfiguration(raw: string | null | undefined): void {
    const { rows, preserved } = this.parseTestConfiguration(raw);
    this.preservedConfig = preserved;
    this.testsArray.clear();
    this.rowKeys = [];
    for (const row of rows) {
      this.testsArray.push(this.buildTestRow(row));
      this.rowKeys.push(this.nextRowKey++);
    }
    const loadedUnits = rows.map((r) => r.unit).filter((u) => u && !CURATED_UNITS.includes(u));
    this.availableUnits = [...CURATED_UNITS, ...new Set(loadedUnits)];
  }

  /** Rebuilds the Test Configuration JSON string, assigning ids to new rows and preserving other keys. */
  private buildTestConfiguration(): string {
    const rows = this.testsArray.controls.map((c) => c.value as TestRowValue);
    let maxId = rows.reduce((max, r) => (r.id !== null && r.id > max ? r.id : max), 0);
    const tests = rows.map((r) => {
      const id = r.id ?? ++maxId;
      const test: Record<string, unknown> = { id, name: r.name, unit: r.unit };
      if (r.method) {
        test['method'] = r.method;
      }
      return test;
    });
    return JSON.stringify({ ...this.preservedConfig, tests });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
      this.templateId = Number(id);
      this.loadTemplate();
    }
  }

  private loadTemplate(): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.getTemplate(this.templateId!).subscribe({
      next: (template) => {
        this.rowVersion = template.rowVersion;
        this.form.patchValue({
          name: template.name,
          minTolerance: template.minTolerance,
          maxTolerance: template.maxTolerance,
          validationRules: template.validationRules,
          calculationDefinitions: template.calculationDefinitions,
        });
        this.applyTestConfiguration(template.testConfiguration);
        this.form.get('site')?.disable();
        this.breadcrumb.set([{ label: 'Templates', link: '/analysis/templates' }, { label: template.name }]);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load template. Please try again.');
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.testsArray.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set('');

    const formValue = this.form.value;
    const testConfiguration = this.buildTestConfiguration();

    if (this.isEdit()) {
      const request: UpdateAnalysisTemplateRequest = {
        name: formValue.name,
        rowVersion: this.rowVersion,
        minTolerance: formValue.minTolerance || undefined,
        maxTolerance: formValue.maxTolerance || undefined,
        testConfiguration,
        validationRules: formValue.validationRules || undefined,
        calculationDefinitions: formValue.calculationDefinitions || undefined,
      };
      this.apiService.updateTemplate(this.templateId!, request).subscribe({
        next: () => {
          this.submitting.set(false);
          this.toast.success(`Template "${request.name}" updated.`);
          this.router.navigate(['../..'], { relativeTo: this.route });
        },
        error: (err) => {
          this.submitting.set(false);
          if (err.status === 400) {
            this.submitError.set(err.error?.detail || 'Validation failed');
          } else if (err.status === 409) {
            this.submitError.set('Template has been modified. Please reload.');
          } else {
            this.submitError.set('Failed to update template. Please try again.');
          }
        },
      });
    } else {
      const request: CreateAnalysisTemplateRequest = {
        name: formValue.name,
        site: formValue.site,
        minTolerance: formValue.minTolerance || undefined,
        maxTolerance: formValue.maxTolerance || undefined,
        testConfiguration,
        validationRules: formValue.validationRules || undefined,
        calculationDefinitions: formValue.calculationDefinitions || undefined,
      };
      this.apiService.createTemplate(request).subscribe({
        next: () => {
          this.submitting.set(false);
          this.toast.success(`Template "${request.name}" created.`);
          this.router.navigate(['..'], { relativeTo: this.route });
        },
        error: (err) => {
          this.submitting.set(false);
          if (err.status === 400) {
            this.submitError.set(err.error?.detail || 'Validation failed');
          } else {
            this.submitError.set('Failed to create template. Please try again.');
          }
        },
      });
    }
  }

  cancel(): void {
    this.router.navigate(['..'], { relativeTo: this.route });
  }
}

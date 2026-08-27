import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { AnalysisExecutionApiService, AnalysisDetailDto, ExceptionDto, InstrumentDto, TestDefinitionDto } from './services/analysis-execution-api.service';
import { StatusChangeRequest } from '../../shared/generated/models/status-change-request';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputComponent } from '@/shared/components/input';
import { ZardSelectComponent, ZardSelectItemComponent } from '@/shared/components/select';
import { ZardCardComponent, ZardCardHeaderComponent, ZardCardTitleComponent, ZardCardContentComponent } from '@/shared/components/card';
import { ZardTextareaComponent } from '@/shared/components/textarea';
import { ZardBadgeComponent } from '@/shared/components/badge';
import { ZardTableImports } from '@/shared/components/table';
import { ZardPaginationComponent } from '@/shared/components/pagination/pagination.component';
import { ZardAlertComponent } from '@/shared/components/alert';
import { ZardEmptyComponent } from '@/shared/components/empty';
import { ZardSpinnerComponent } from '@/shared/components/spinner';
import { StatusBadgeComponent } from '@/shared/ui/status-badge/status-badge.component';
import { ToastService } from '@/shared/services/toast/toast.service';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideAlertCircle,
  lucideChevronDown,
  lucideRefreshCw,
  lucideCheck,
  lucidePlay,
  lucidePause,
  lucideCircleCheck,
  lucideX,
} from '@ng-icons/lucide';

@Component({
  selector: 'lims-analysis-execution',
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
    ZardCardContentComponent,
    ZardTextareaComponent,
    ZardBadgeComponent,
    ZardAlertComponent,
    ZardEmptyComponent,
    ZardSpinnerComponent,
    StatusBadgeComponent,
    ...ZardTableImports,
    ZardPaginationComponent,
    NgIcon,
  ],
  templateUrl: './analysis-execution.component.html',
  styleUrl: './analysis-execution.component.scss',
  viewProviders: [
    provideIcons({
      lucideAlertCircle,
      lucideChevronDown,
      lucideRefreshCw,
      lucideCheck,
      lucidePlay,
      lucidePause,
      lucideCircleCheck,
      lucideX,
    }),
  ],
})
export class AnalysisExecutionComponent implements OnInit {
  private fb = inject(FormBuilder);
  private apiService = inject(AnalysisExecutionApiService);
  private route = inject(ActivatedRoute);
  private toast = inject(ToastService);

  /** Past-tense confirmation copy per lifecycle action (matches the button that triggered it). */
  private static readonly STATUS_TOASTS: Record<string, string> = {
    Start: 'Analysis started.',
    Pause: 'Analysis paused.',
    Resume: 'Analysis resumed.',
    Complete: 'Analysis completed and locked.',
    Cancel: 'Analysis cancelled.',
  };

  loading = signal(false);
  error = signal('');
  analysis = signal<AnalysisDetailDto | null>(null);
  instruments = signal<InstrumentDto[]>([]);
  instrumentsLoading = signal(false);
  submittingReading = signal(false);
  readingError = signal('');
  submittingException = signal(false);
  exceptionError = signal('');
  staleRowVersionError = signal(false);
  changingStatus = signal(false);
  statusError = signal('');

  decisionOptions: string[] = ['Modify', 'Retest', 'AcceptWithComment'];

  /** Leading icon for a lifecycle action button (BRD R20). */
  private static readonly ACTION_ICONS: Record<string, string> = {
    Start: 'lucidePlay',
    Resume: 'lucidePlay',
    Pause: 'lucidePause',
    Complete: 'lucideCircleCheck',
    Cancel: 'lucideX',
  };

  lifecycleIcon(action: string): string {
    return AnalysisExecutionComponent.ACTION_ICONS[action] ?? 'lucideCheck';
  }

  isLocked = computed(() => {
    return this.analysis()?.isLocked ?? false;
  });

  activeInstruments = computed(() => {
    return this.instruments().filter(i => i.isActive);
  });

  availableTests = computed<TestDefinitionDto[]>(() => {
    return this.analysis()?.availableTests ?? [];
  });

  readings = computed(() => this.analysis()?.readings ?? []);
  pageSize = 10;
  pageIndex = signal(1);
  totalPages = computed(() => Math.max(1, Math.ceil(this.readings().length / this.pageSize)));
  pagedReadings = computed(() => {
    const start = (this.pageIndex() - 1) * this.pageSize;
    return this.readings().slice(start, start + this.pageSize);
  });

  // Valid lifecycle actions for the current status (BRD R20). Server re-checks.
  availableActions = computed<string[]>(() => {
    switch (this.analysis()?.status) {
      case 'NotStarted':
        return ['Start', 'Cancel'];
      case 'InProgress':
        return ['Pause', 'Complete', 'Cancel'];
      case 'OnHold':
        return ['Resume', 'Cancel'];
      default:
        return [];
    }
  });

  readingForm: FormGroup;
  private exceptionForms = new Map<number, FormGroup>();
  private currentAnalysisId = 0;

  constructor() {
    this.readingForm = this.fb.group({
      testId: ['', Validators.required],
      value: ['', [Validators.required, Validators.pattern(/^\d+(\.\d{1,2})?$/)]],
      unit: ['', Validators.required],
      capturedAtUtc: [null, Validators.required],
      instrumentId: [''],
    });

    this.testIdControl.valueChanges.subscribe((testId) => this.applyUnitForTest(testId));
  }

  /** Auto-fills the read-only unit control from the selected test's definition (BRD: unit is not free-typed). */
  private applyUnitForTest(testId: string | number | null): void {
    const tests = this.availableTests();
    if (tests.length === 0 || testId === null || testId === '') {
      return;
    }
    const test = tests.find((t) => t.id.toString() === testId.toString());
    if (test) {
      this.unitControl.setValue(test.unit, { emitEvent: false });
    }
  }

  /** Displays the test name in the readings table instead of the raw test ID. */
  testName(testId: string | number): string {
    const test = this.availableTests().find((t) => t.id.toString() === testId.toString());
    return test ? test.name : testId.toString();
  }

  ngOnInit(): void {
    this.currentAnalysisId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadAnalysis(this.currentAnalysisId);
    this.loadInstruments();
  }

  private loadAnalysis(analysisId: number): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.getAnalysis(analysisId).subscribe({
      next: (data) => {
        this.analysis.set(data);
        this.pageIndex.set(1);
        this.loading.set(false);
        const tests = data?.availableTests ?? [];
        if (tests.length === 1) {
          this.testIdControl.setValue(tests[0].id.toString());
        }
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.error.set('Analysis not found');
        } else {
          this.error.set('Failed to load analysis. Please try again.');
        }
      },
    });
  }

  private loadInstruments(): void {
    this.instrumentsLoading.set(true);
    this.apiService.getInstruments().subscribe({
      next: (data) => {
        this.instruments.set(data);
        this.instrumentsLoading.set(false);
      },
      error: () => {
        // Instruments are optional; don't block the form if they fail to load
        this.instrumentsLoading.set(false);
      },
    });
  }

  reload(): void {
    this.loadAnalysis(this.currentAnalysisId);
  }

  changeStatus(action: string): void {
    const current = this.analysis();
    if (!current || this.changingStatus()) {
      return;
    }
    this.changingStatus.set(true);
    this.statusError.set('');
    const request: StatusChangeRequest = { action, rowVersion: current.rowVersion };
    this.apiService.changeStatus(current.id, request).subscribe({
      next: () => {
        this.changingStatus.set(false);
        this.toast.success(AnalysisExecutionComponent.STATUS_TOASTS[action] ?? 'Analysis updated.');
        this.loadAnalysis(current.id);
      },
      error: (err) => {
        this.changingStatus.set(false);
        if (err.status === 409) {
          this.staleRowVersionError.set(true);
          this.statusError.set('This analysis was modified. Please reload.');
        } else {
          this.statusError.set(err.error?.detail || 'Failed to change status. Please try again.');
        }
        this.toast.error('Could not update the analysis status.');
      },
    });
  }

  get testIdControl() {
    return this.readingForm.get('testId')!;
  }

  get valueControl() {
    return this.readingForm.get('value')!;
  }

  get unitControl() {
    return this.readingForm.get('unit')!;
  }

  get capturedAtUtcControl() {
    return this.readingForm.get('capturedAtUtc')!;
  }

  get instrumentIdControl() {
    return this.readingForm.get('instrumentId')!;
  }

  submitReading(): void {
    if (this.readingForm.invalid || !this.analysis()) {
      return;
    }

    this.submittingReading.set(true);
    this.readingError.set('');

    const analysisId = this.analysis()!.id;
    const formValue = this.readingForm.value;

    const request = {
      testId: Number(formValue.testId),
      value: Number(formValue.value),
      unit: formValue.unit,
      capturedAtUtc: formValue.capturedAtUtc,
      instrumentId: formValue.instrumentId ? Number(formValue.instrumentId) : undefined,
    };

    this.apiService.addReading(analysisId, request).subscribe({
      next: () => {
        this.submittingReading.set(false);
        this.toast.success('Reading captured.');
        this.readingForm.reset();
        this.loadAnalysis(analysisId);
      },
      error: (err) => {
        this.submittingReading.set(false);
        if (err.status === 400) {
          this.readingError.set(err.error?.detail || 'Validation failed');
        } else {
          this.readingError.set('Failed to submit reading. Please try again.');
        }
      },
    });
  }

  getExceptionForm(exceptionId: number): FormGroup {
    if (!this.exceptionForms.has(exceptionId)) {
      this.exceptionForms.set(
        exceptionId,
        this.fb.group({
          decision: ['', Validators.required],
          comment: ['', Validators.required],
        })
      );
    }
    return this.exceptionForms.get(exceptionId)!;
  }

  resolveException(exception: ExceptionDto): void {
    const form = this.getExceptionForm(exception.id);
    if (form.invalid || !this.analysis()) {
      return;
    }

    this.submittingException.set(true);
    this.exceptionError.set('');

    const analysisId = this.analysis()!.id;
    const request = {
      decision: form.value.decision,
      comment: form.value.comment,
      rowVersion: exception.rowVersion,
    };

    this.apiService.resolveException(analysisId, exception.id, request).subscribe({
      next: () => {
        this.submittingException.set(false);
        this.toast.success('Exception resolved.');
        form.reset();
        this.exceptionForms.delete(exception.id);
        this.loadAnalysis(analysisId);
      },
      error: (err) => {
        this.submittingException.set(false);
        if (err.status === 409) {
          this.staleRowVersionError.set(true);
          this.exceptionError.set('This analysis was modified. Please reload.');
        } else if (err.status === 400) {
          this.exceptionError.set(err.error?.detail || 'Validation failed');
        } else {
          this.exceptionError.set('Failed to resolve exception. Please try again.');
        }
      },
    });
  }
}

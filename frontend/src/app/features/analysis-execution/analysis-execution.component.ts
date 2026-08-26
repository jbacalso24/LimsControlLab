import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { TextBoxModule } from '@progress/kendo-angular-inputs';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { DropDownListModule } from '@progress/kendo-angular-dropdowns';
import { AnalysisExecutionApiService, AnalysisDetailDto, ExceptionDto } from './services/analysis-execution-api.service';

@Component({
  selector: 'lims-analysis-execution',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    TextBoxModule,
    DateInputsModule,
    DropDownListModule,
  ],
  templateUrl: './analysis-execution.component.html',
  styleUrl: './analysis-execution.component.scss',
})
export class AnalysisExecutionComponent implements OnInit {
  private fb = inject(FormBuilder);
  private apiService = inject(AnalysisExecutionApiService);
  private route = inject(ActivatedRoute);

  loading = signal(false);
  error = signal('');
  analysis = signal<AnalysisDetailDto | null>(null);
  submittingReading = signal(false);
  readingError = signal('');
  submittingException = signal(false);
  exceptionError = signal('');
  staleRowVersionError = signal(false);

  decisionOptions: string[] = ['Modify', 'Retest', 'AcceptWithComment'];

  isLocked = computed(() => {
    return this.analysis()?.isLocked ?? false;
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
  }

  ngOnInit(): void {
    this.currentAnalysisId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadAnalysis(this.currentAnalysisId);
  }

  private loadAnalysis(analysisId: number): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.getAnalysis(analysisId).subscribe({
      next: (data) => {
        this.analysis.set(data);
        this.loading.set(false);
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

  reload(): void {
    this.loadAnalysis(this.currentAnalysisId);
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

  submitReading(): void {
    if (this.readingForm.invalid || !this.analysis()) {
      return;
    }

    this.submittingReading.set(true);
    this.readingError.set('');

    const analysisId = this.analysis()!.id;
    const formValue = this.readingForm.value;

    const request = {
      testId: formValue.testId,
      value: Number(formValue.value),
      unit: formValue.unit,
      capturedAtUtc: formValue.capturedAtUtc,
      instrumentId: formValue.instrumentId || undefined,
    };

    this.apiService.addReading(analysisId, request).subscribe({
      next: () => {
        this.submittingReading.set(false);
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

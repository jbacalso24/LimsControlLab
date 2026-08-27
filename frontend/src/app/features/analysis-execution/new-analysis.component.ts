import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardInputComponent } from '@/shared/components/input/input.component';
import { ZardSelectComponent, ZardSelectItemComponent } from '@/shared/components/select';
import { ZardCardComponent, ZardCardHeaderComponent, ZardCardTitleComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import { ToastService } from '@/shared/services/toast/toast.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { TemplatesApiService } from '../templates/services/templates-api.service';
import { AnalysisTemplateDto } from '../../shared/generated/models/analysis-template-dto';
import { AnalysisExecutionApiService, CreateAnalysisRequest } from './services/analysis-execution-api.service';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideX } from '@ng-icons/lucide';

@Component({
  selector: 'lims-new-analysis',
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
    ZardAlertComponent,
    ZardSpinnerComponent,
    ZardEmptyComponent,
    NgIcon,
  ],
  templateUrl: './new-analysis.component.html',
  viewProviders: [provideIcons({ lucidePlus, lucideX })],
})
export class NewAnalysisComponent implements OnInit {
  private fb = inject(FormBuilder);
  private templatesApi = inject(TemplatesApiService);
  private analysisApi = inject(AnalysisExecutionApiService);
  private currentUser = inject(CurrentUserService);
  private toast = inject(ToastService);
  private router = inject(Router);

  form: FormGroup;
  loading = signal(false);
  error = signal('');
  submitting = signal(false);
  submitError = signal('');
  private allTemplates = signal<AnalysisTemplateDto[]>([]);

  templates = computed(() => {
    const site = this.currentUser.user()?.site;
    return this.allTemplates().filter((t) => t.site === site && !t.isRetired);
  });

  constructor() {
    this.form = this.fb.group({
      analysisTemplateId: ['', Validators.required],
      sampleIdentifier: [''],
    });
  }

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set('');
    this.templatesApi.listTemplates().subscribe({
      next: (templates) => {
        this.allTemplates.set(templates);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load templates. Please try again.');
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set('');

    const formValue = this.form.value;
    const request: CreateAnalysisRequest = {
      analysisTemplateId: Number(formValue.analysisTemplateId),
      sampleIdentifier: formValue.sampleIdentifier || null,
    };

    this.analysisApi.createAnalysis(request).subscribe({
      next: (created) => {
        this.submitting.set(false);
        this.toast.success('Analysis created for ' + created.sampleIdentifier);
        this.router.navigate(['/analysis/analysis', created.analysisId]);
      },
      error: (err) => {
        this.submitting.set(false);
        if (err.status === 400) {
          this.submitError.set(err.error?.detail || 'Validation failed');
        } else {
          this.submitError.set('Failed to create analysis. Please try again.');
        }
      },
    });
  }

  cancel(): void {
    this.router.navigate(['/analysis/work-queue']);
  }
}

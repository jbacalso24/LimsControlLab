import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardInputComponent } from '@/shared/components/input/input.component';
import { ZardSelectComponent, ZardSelectItemComponent } from '@/shared/components/select';
import { ZardTextareaComponent } from '@/shared/components/textarea/textarea.component';
import { ZardCardComponent, ZardCardHeaderComponent, ZardCardTitleComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { TemplatesApiService } from './services/templates-api.service';
import { CreateAnalysisTemplateRequest } from '../../shared/generated/models/create-analysis-template-request';
import { UpdateAnalysisTemplateRequest } from '../../shared/generated/models/update-analysis-template-request';
import { NgIcon } from '@ng-icons/core';

const SITES = ['Inkerman', 'Invicta', 'Kalamia', 'Victoria', 'Macknade', 'Proserpine', 'PlaneCreek', 'Pioneer'];

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
    ZardTextareaComponent,
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
})
export class TemplatesFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private apiService = inject(TemplatesApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  sites = SITES;
  form: FormGroup;
  loading = signal(false);
  submitting = signal(false);
  error = signal('');
  submitError = signal('');
  isEdit = signal(false);
  private templateId: number | null = null;
  private rowVersion = '';

  constructor() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      site: ['', Validators.required],
      minTolerance: [''],
      maxTolerance: [''],
      testConfiguration: [''],
      validationRules: [''],
      calculationDefinitions: [''],
    });
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
          testConfiguration: template.testConfiguration,
          validationRules: template.validationRules,
          calculationDefinitions: template.calculationDefinitions,
        });
        this.form.get('site')?.disable();
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
      return;
    }

    this.submitting.set(true);
    this.submitError.set('');

    const formValue = this.form.value;

    if (this.isEdit()) {
      const request: UpdateAnalysisTemplateRequest = {
        name: formValue.name,
        rowVersion: this.rowVersion,
        minTolerance: formValue.minTolerance || undefined,
        maxTolerance: formValue.maxTolerance || undefined,
        testConfiguration: formValue.testConfiguration || undefined,
        validationRules: formValue.validationRules || undefined,
        calculationDefinitions: formValue.calculationDefinitions || undefined,
      };
      this.apiService.updateTemplate(this.templateId!, request).subscribe({
        next: () => {
          this.submitting.set(false);
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
        testConfiguration: formValue.testConfiguration || undefined,
        validationRules: formValue.validationRules || undefined,
        calculationDefinitions: formValue.calculationDefinitions || undefined,
      };
      this.apiService.createTemplate(request).subscribe({
        next: () => {
          this.submitting.set(false);
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

import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputComponent } from '@/shared/components/input';
import { ZardSelectComponent, ZardSelectItemComponent } from '@/shared/components/select';
import { ZardCardComponent, ZardCardContentComponent } from '@/shared/components/card';
import { ZardSpinnerComponent } from '@/shared/components/spinner';
import { ToastService } from '@/shared/services/toast/toast.service';
import { SchedulingApiService } from './services/scheduling-api.service';
import { CreateScheduleRequest } from '../../shared/generated/models/create-schedule-request';
import { UpdateScheduleRequest } from '../../shared/generated/models/update-schedule-request';

const SITES = ['Inkerman', 'Invicta', 'Kalamia', 'Victoria', 'Macknade', 'Proserpine', 'PlaneCreek', 'Pioneer'];
const SHIFT_PATTERNS = ['Day', 'Shift', 'Weekly'];

@Component({
  selector: 'lims-scheduling-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardButtonComponent,
    ZardInputComponent,
    ZardSelectComponent,
    ZardSelectItemComponent,
    ZardCardComponent,
    ZardCardContentComponent,
    ZardSpinnerComponent,
  ],
  templateUrl: './scheduling-form.component.html',
  styleUrl: './scheduling-form.component.scss',
})
export class SchedulingFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private apiService = inject(SchedulingApiService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);

  sites = SITES;
  shiftPatterns = SHIFT_PATTERNS;
  form: FormGroup;
  loading = signal(false);
  submitting = signal(false);
  error = signal('');
  submitError = signal('');
  isEdit = signal(false);
  private scheduleId: number | null = null;
  private rowVersion = '';
  private assignedToUserId: number | string | null | undefined = undefined;

  constructor() {
    this.form = this.fb.group({
      name: ['', Validators.required],
      site: ['', Validators.required],
      shiftPattern: ['', Validators.required],
      analysisType: [''],
      recurrencePattern: [''],
      exclusionRules: [''],
      assignedToUserId: [''],
      isActive: [true],
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
      this.scheduleId = Number(id);
      this.loadSchedule();
    }
  }

  private loadSchedule(): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.getSchedule(this.scheduleId!).subscribe({
      next: (schedule) => {
        this.rowVersion = schedule.rowVersion;
        this.assignedToUserId = schedule.assignedToUserId;
        this.form.patchValue({
          name: schedule.name,
          site: schedule.site,
          shiftPattern: schedule.shiftPattern,
          analysisType: schedule.analysisType,
          recurrencePattern: schedule.recurrencePattern,
          exclusionRules: schedule.exclusionRules,
          assignedToUserId: schedule.assignedToUserId || '',
          isActive: schedule.isActive,
        });
        this.form.get('site')?.disable();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load schedule. Please try again.');
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
      const request: UpdateScheduleRequest = {
        name: formValue.name,
        shiftPattern: formValue.shiftPattern,
        isActive: formValue.isActive,
        rowVersion: this.rowVersion,
        analysisType: formValue.analysisType || undefined,
        recurrencePattern: formValue.recurrencePattern || undefined,
        exclusionRules: formValue.exclusionRules || undefined,
        assignedToUserId: this.assignedToUserId,
      };
      this.apiService.updateSchedule(this.scheduleId!, request).subscribe({
        next: () => {
          this.submitting.set(false);
          this.toast.success(`Schedule "${request.name}" updated.`);
          this.router.navigate(['../..'], { relativeTo: this.route });
        },
        error: (err) => {
          this.submitting.set(false);
          if (err.status === 400) {
            this.submitError.set(err.error?.detail || 'Validation failed');
          } else if (err.status === 409) {
            this.submitError.set('Schedule has been modified. Please reload.');
          } else {
            this.submitError.set('Failed to update schedule. Please try again.');
          }
        },
      });
    } else {
      const request: CreateScheduleRequest = {
        name: formValue.name,
        site: formValue.site,
        shiftPattern: formValue.shiftPattern,
        analysisType: formValue.analysisType || undefined,
        recurrencePattern: formValue.recurrencePattern || undefined,
        exclusionRules: formValue.exclusionRules || undefined,
        assignedToUserId: undefined,
      };
      this.apiService.createSchedule(request).subscribe({
        next: () => {
          this.submitting.set(false);
          this.toast.success(`Schedule "${request.name}" created.`);
          this.router.navigate(['..'], { relativeTo: this.route });
        },
        error: (err) => {
          this.submitting.set(false);
          if (err.status === 400) {
            this.submitError.set(err.error?.detail || 'Validation failed');
          } else {
            this.submitError.set('Failed to create schedule. Please try again.');
          }
        },
      });
    }
  }

  cancel(): void {
    this.router.navigate(['..'], { relativeTo: this.route });
  }
}

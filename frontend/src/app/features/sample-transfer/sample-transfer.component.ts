import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideAlertCircle } from '@ng-icons/lucide';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardSelectComponent, ZardSelectItemComponent } from '@/shared/components/select';
import { ZardCardComponent, ZardCardContentComponent, ZardCardHeaderComponent, ZardCardTitleComponent } from '@/shared/components/card';
import { ZardAlertComponent } from '@/shared/components/alert';
import { StatusBadgeComponent } from '@/shared/ui/status-badge/status-badge.component';
import { SampleTransferApiService } from './services/sample-transfer-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { SampleDto } from '../../shared/generated/models/sample-dto';

const SITES = ['Inkerman', 'Invicta', 'Kalamia', 'Victoria', 'Macknade', 'Proserpine', 'PlaneCreek', 'Pioneer'];

@Component({
  selector: 'lims-sample-transfer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgIcon,
    ZardButtonComponent,
    ZardSelectComponent,
    ZardSelectItemComponent,
    ZardCardComponent,
    ZardCardContentComponent,
    ZardCardHeaderComponent,
    ZardCardTitleComponent,
    ZardAlertComponent,
    StatusBadgeComponent,
  ],
  templateUrl: './sample-transfer.component.html',
  styleUrl: './sample-transfer.component.scss',
  viewProviders: [provideIcons({ lucideAlertCircle })],
})
export class SampleTransferComponent implements OnInit {
  private apiService = inject(SampleTransferApiService);
  private currentUserService = inject(CurrentUserService);
  private formBuilder = inject(FormBuilder);
  private route = inject(ActivatedRoute);

  loading = signal(false);
  error404 = signal(false);
  error403 = signal(false);
  errorOther = signal('');
  sample = signal<SampleDto | null>(null);
  showTransferDialog = signal(false);
  transferring = signal(false);
  transferError = signal('');
  staleRowVersionError = signal(false);

  sites = SITES;
  availableSites = SITES;
  transferForm = this.createTransferForm();

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      const sampleId = params.get('id');
      if (sampleId) {
        this.loadSample(Number(sampleId));
      }
    });
  }

  private createTransferForm(): FormGroup {
    return this.formBuilder.group({
      toSite: ['', [Validators.required]],
    });
  }

  private loadSample(sampleId: number): void {
    this.loading.set(true);
    this.error404.set(false);
    this.error403.set(false);
    this.errorOther.set('');
    this.apiService.getSample(sampleId).subscribe({
      next: (data) => {
        this.sample.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.error404.set(true);
        } else if (err.status === 403) {
          this.error403.set(true);
        } else {
          this.errorOther.set('Failed to load sample. Please try again.');
        }
      },
    });
  }

  canTransfer(): boolean {
    const currentUser = this.currentUserService.user();
    const sampleData = this.sample();
    return currentUser?.site === sampleData?.currentSite;
  }

  openTransferDialog(): void {
    this.transferForm = this.createTransferForm();
    this.transferError.set('');
    this.staleRowVersionError.set(false);
    this.showTransferDialog.set(true);
  }

  closeTransferDialog(): void {
    this.showTransferDialog.set(false);
    this.transferForm.reset();
    this.transferError.set('');
    this.staleRowVersionError.set(false);
  }

  submitTransfer(): void {
    if (!this.transferForm.valid) {
      return;
    }

    const sampleData = this.sample();
    if (!sampleData) {
      return;
    }

    this.transferring.set(true);
    this.transferError.set('');
    this.staleRowVersionError.set(false);

    const request = {
      toSite: this.transferForm.get('toSite')?.value || '',
      rowVersion: sampleData.rowVersion,
    };

    this.apiService.transferSample(Number(sampleData.id), request).subscribe({
      next: () => {
        this.transferring.set(false);
        this.closeTransferDialog();
        this.loadSample(Number(sampleData.id));
      },
      error: (err) => {
        this.transferring.set(false);
        if (err.status === 409) {
          this.staleRowVersionError.set(true);
        } else {
          this.transferError.set(
            err.error?.message || 'Failed to transfer sample. Please try again.'
          );
        }
      },
    });
  }

  retry(): void {
    const sampleData = this.sample();
    if (sampleData) {
      this.loadSample(Number(sampleData.id));
    }
  }

  goBack(): void {
    window.history.back();
  }
}

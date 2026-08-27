import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideAlertCircle, lucideArrowLeftRight, lucideRefreshCw, lucideArrowLeft, lucideX } from '@ng-icons/lucide';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardInputComponent } from '@/shared/components/input';
import { ZardSelectComponent, ZardSelectItemComponent } from '@/shared/components/select';
import { ZardCardComponent, ZardCardContentComponent, ZardCardHeaderComponent, ZardCardTitleComponent } from '@/shared/components/card';
import { ZardAlertComponent } from '@/shared/components/alert';
import { ZardSpinnerComponent } from '@/shared/components/spinner';
import { ZardEmptyComponent } from '@/shared/components/empty';
import { ZardTableImports } from '@/shared/components/table';
import { ZardPaginationComponent } from '@/shared/components/pagination/pagination.component';
import { StatusBadgeComponent } from '@/shared/ui/status-badge/status-badge.component';
import { DetailDialogComponent, DetailRow } from '@/shared/ui/detail-dialog/detail-dialog.component';
import { ToastService } from '@/shared/services/toast/toast.service';
import { BreadcrumbService } from '@/shared/services/breadcrumb/breadcrumb.service';
import { SampleTransferApiService } from './services/sample-transfer-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { SampleDto } from '../../shared/generated/models/sample-dto';

interface PickerSample {
  sampleId: number;
  identifier: string;
  site: string;
  status: string;
  templateName: string;
}

const SITES = ['Inkerman', 'Invicta', 'Kalamia', 'Victoria', 'Macknade', 'Proserpine', 'PlaneCreek', 'Pioneer'];

@Component({
  selector: 'lims-sample-transfer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    NgIcon,
    ZardButtonComponent,
    ZardInputComponent,
    ZardSelectComponent,
    ZardSelectItemComponent,
    ZardCardComponent,
    ZardCardContentComponent,
    ZardCardHeaderComponent,
    ZardCardTitleComponent,
    ZardAlertComponent,
    ZardSpinnerComponent,
    ZardEmptyComponent,
    StatusBadgeComponent,
    ...ZardTableImports,
    ZardPaginationComponent,
    DetailDialogComponent,
  ],
  templateUrl: './sample-transfer.component.html',
  styleUrl: './sample-transfer.component.scss',
  viewProviders: [provideIcons({ lucideAlertCircle, lucideArrowLeftRight, lucideRefreshCw, lucideArrowLeft, lucideX })],
})
export class SampleTransferComponent implements OnInit {
  private apiService = inject(SampleTransferApiService);
  private currentUserService = inject(CurrentUserService);
  private formBuilder = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toast = inject(ToastService);
  private breadcrumb = inject(BreadcrumbService);

  // Picker mode (no :id in the route) — choose a sample to transfer.
  pickerMode = signal(false);
  pickerLoading = signal(false);
  pickerError = signal('');
  pickerSamples = signal<PickerSample[]>([]);
  pickerFilter = signal('');
  filteredPickerSamples = computed(() => {
    const q = this.pickerFilter().trim().toLowerCase();
    const rows = this.pickerSamples();
    if (!q) return rows;
    return rows.filter(
      (s) => s.identifier.toLowerCase().includes(q) || s.templateName.toLowerCase().includes(q),
    );
  });

  pageSize = 10;
  pageIndex = signal(1);
  totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredPickerSamples().length / this.pageSize)),
  );
  pagedPickerSamples = computed(() => {
    const start = (this.pageIndex() - 1) * this.pageSize;
    return this.filteredPickerSamples().slice(start, start + this.pageSize);
  });

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
        this.pickerMode.set(false);
        this.sample.set(null);
        this.loadSample(Number(sampleId));
      } else {
        this.pickerMode.set(true);
        this.sample.set(null);
        this.loadPicker();
      }
    });
  }

  private loadPicker(): void {
    this.pickerLoading.set(true);
    this.pickerError.set('');
    this.apiService.listSamplesForPicker().subscribe({
      next: (res) => {
        const seen = new Set<number>();
        const rows: PickerSample[] = [];
        for (const item of res.items ?? []) {
          const id = Number(item.sampleId);
          if (seen.has(id)) continue;
          seen.add(id);
          rows.push({
            sampleId: id,
            identifier: item.sampleIdentifier,
            site: item.site,
            status: item.status,
            templateName: item.templateName,
          });
        }
        rows.sort((a, b) => a.identifier.localeCompare(b.identifier));
        this.pickerSamples.set(rows);
        this.pageIndex.set(1);
        this.pickerLoading.set(false);
      },
      error: () => {
        this.pickerLoading.set(false);
        this.pickerError.set('Could not load samples. Please try again.');
      },
    });
  }

  reloadPicker(): void {
    this.loadPicker();
  }

  openSample(sampleId: number): void {
    this.router.navigate(['/analysis/sample-transfer', sampleId]);
  }

  /** Row selected for the picker details modal. */
  selectedPickerSample = signal<PickerSample | null>(null);

  detailRows(row: PickerSample): DetailRow[] {
    return [
      { label: 'Sample', value: row.identifier },
      { label: 'Template', value: row.templateName },
      { label: 'Site', value: row.site },
      { label: 'Status', value: row.status },
    ];
  }

  onPickerFilter(value: string): void {
    this.pickerFilter.set(value);
    this.pageIndex.set(1);
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
        if (data) {
          this.breadcrumb.set([
            { label: 'Sample Transfer', link: '/analysis/sample-transfer' },
            { label: data.identifier },
          ]);
        }
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
        this.toast.success(`Sample ${sampleData.identifier} transferred to ${request.toSite}.`);
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

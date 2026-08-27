import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
} from '@angular/forms';
import { ExceptionReviewApiService } from './services/exception-review-api.service';
import { ResultReviewDto } from '../../shared/generated/models/result-review-dto';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardTextareaComponent } from '@/shared/components/textarea/textarea.component';
import { ZardTableComponent, ZardTableHeaderComponent, ZardTableBodyComponent, ZardTableRowComponent, ZardTableHeadComponent, ZardTableCellComponent } from '@/shared/components/table/table.component';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import { ZardCardComponent, ZardCardHeaderComponent, ZardCardTitleComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardPaginationComponent } from '@/shared/components/pagination/pagination.component';
import { StatusBadgeComponent } from '@/shared/ui/status-badge/status-badge.component';
import { ToastService } from '@/shared/services/toast/toast.service';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideAlertCircle, lucideLock, lucideCircleCheck, lucideRefreshCw, lucideLockOpen, lucideX } from '@ng-icons/lucide';

@Component({
  selector: 'lims-exception-review-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardButtonComponent,
    ZardTextareaComponent,
    ZardTableComponent,
    ZardTableHeaderComponent,
    ZardTableBodyComponent,
    ZardTableRowComponent,
    ZardTableHeadComponent,
    ZardTableCellComponent,
    ZardSpinnerComponent,
    ZardAlertComponent,
    ZardEmptyComponent,
    ZardCardComponent,
    ZardCardHeaderComponent,
    ZardCardTitleComponent,
    ZardCardContentComponent,
    ZardPaginationComponent,
    StatusBadgeComponent,
    NgIcon,
  ],
  templateUrl: './exception-review-list.component.html',
  styleUrl: './exception-review-list.component.scss',
  viewProviders: [provideIcons({ lucideAlertCircle, lucideLock, lucideCircleCheck, lucideRefreshCw, lucideLockOpen, lucideX })],
})
export class ExceptionReviewListComponent {
  private apiService = inject(ExceptionReviewApiService);
  private currentUserService = inject(CurrentUserService);
  private formBuilder = inject(FormBuilder);
  private toast = inject(ToastService);

  loading = signal(false);
  error = signal('');
  forbidden = signal(false);
  analyses = signal<ResultReviewDto[]>([]);

  pageSize = 10;
  pageIndex = signal(1);
  totalPages = computed(() => Math.max(1, Math.ceil(this.analyses().length / this.pageSize)));
  pagedAnalyses = computed(() => {
    const start = (this.pageIndex() - 1) * this.pageSize;
    return this.analyses().slice(start, start + this.pageSize);
  });
  showUnlockDialog = signal(false);
  unlocking = signal(false);
  unlockError = signal('');
  staleRowVersionError = signal(false);
  selectedAnalysis = signal<ResultReviewDto | null>(null);

  unlockForm = this.createUnlockForm();

  ngOnInit(): void {
    this.loadAnalyses();
  }

  private createUnlockForm(): FormGroup {
    return this.formBuilder.group({
      justification: ['', [Validators.required]],
    });
  }

  private loadAnalyses(): void {
    this.loading.set(true);
    this.error.set('');
    this.forbidden.set(false);
    this.apiService.listExceptionAnalyses().subscribe({
      next: (data) => {
        this.analyses.set(data);
        this.pageIndex.set(1);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 403) {
          this.forbidden.set(true);
        } else {
          this.error.set('Failed to load exception analyses. Please try again.');
        }
      },
    });
  }

  reload(): void {
    this.loadAnalyses();
  }

  isLabCoordinator(): boolean {
    return this.currentUserService.user()?.role === 'LabCoordinator';
  }

  openUnlockDialog(analysis: ResultReviewDto): void {
    this.selectedAnalysis.set(analysis);
    this.unlockForm = this.createUnlockForm();
    this.unlockError.set('');
    this.staleRowVersionError.set(false);
    this.showUnlockDialog.set(true);
  }

  closeUnlockDialog(): void {
    this.showUnlockDialog.set(false);
    this.selectedAnalysis.set(null);
    this.unlockForm.reset();
    this.unlockError.set('');
    this.staleRowVersionError.set(false);
  }

  submitUnlock(): void {
    if (!this.unlockForm.valid || !this.selectedAnalysis()) {
      return;
    }

    const analysis = this.selectedAnalysis();
    if (!analysis) {
      return;
    }

    this.unlocking.set(true);
    this.unlockError.set('');
    this.staleRowVersionError.set(false);

    const request = {
      justification: this.unlockForm.get('justification')?.value || '',
      rowVersion: analysis.rowVersion,
    };

    this.apiService.unlockResult(Number(analysis.id), request).subscribe({
      next: () => {
        this.unlocking.set(false);
        this.toast.success(`Result for sample ${analysis.sampleId} unlocked.`);
        this.closeUnlockDialog();
        this.loadAnalyses();
      },
      error: (err) => {
        this.unlocking.set(false);
        if (err.status === 409) {
          this.staleRowVersionError.set(true);
        } else {
          this.unlockError.set(
            err.error?.message || 'Failed to unlock result. Please try again.'
          );
        }
      },
    });
  }
}

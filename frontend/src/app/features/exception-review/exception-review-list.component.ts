import { Component, inject, signal } from '@angular/core';
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
import { StatusBadgeComponent } from '@/shared/ui/status-badge/status-badge.component';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideAlertCircle } from '@ng-icons/lucide';

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
    StatusBadgeComponent,
    NgIcon,
  ],
  templateUrl: './exception-review-list.component.html',
  styleUrl: './exception-review-list.component.scss',
  viewProviders: [provideIcons({ lucideAlertCircle })],
})
export class ExceptionReviewListComponent {
  private apiService = inject(ExceptionReviewApiService);
  private currentUserService = inject(CurrentUserService);
  private formBuilder = inject(FormBuilder);

  loading = signal(false);
  error = signal('');
  analyses = signal<ResultReviewDto[]>([]);
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
    this.apiService.listExceptionAnalyses().subscribe({
      next: (data) => {
        this.analyses.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load exception analyses. Please try again.');
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

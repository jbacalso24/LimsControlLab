import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  IntegrationMonitoringApiService,
  IntegrationLogDto,
} from './services/integration-monitoring-api.service';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import {
  ZardTableComponent,
  ZardTableHeaderComponent,
  ZardTableBodyComponent,
  ZardTableRowComponent,
  ZardTableHeadComponent,
  ZardTableCellComponent,
} from '@/shared/components/table/table.component';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import { ZardCardComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardPaginationComponent } from '@/shared/components/pagination/pagination.component';
import { ZardSelectImports } from '@/shared/components/select/select.imports';
import { DetailDialogComponent, DetailRow } from '@/shared/ui/detail-dialog/detail-dialog.component';
import { ToastService } from '@/shared/services/toast/toast.service';
import { DatePipe } from '@angular/common';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideLock,
  lucidePlugZap,
  lucideCircleAlert,
  lucideClock,
  lucideCircleCheck,
  lucideRefreshCw,
} from '@ng-icons/lucide';

type StatusFilter = 'All' | 'Pending' | 'Success' | 'Failed';

@Component({
  selector: 'lims-integration-monitoring',
  standalone: true,
  imports: [
    CommonModule,
    ZardButtonComponent,
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
    ZardCardContentComponent,
    ZardPaginationComponent,
    ZardSelectImports,
    DetailDialogComponent,
    NgIcon,
  ],
  templateUrl: './integration-monitoring.component.html',
  viewProviders: [
    provideIcons({
      lucideLock,
      lucidePlugZap,
      lucideCircleAlert,
      lucideClock,
      lucideCircleCheck,
      lucideRefreshCw,
    }),
  ],
})
export class IntegrationMonitoringComponent {
  private apiService = inject(IntegrationMonitoringApiService);
  private currentUserService = inject(CurrentUserService);
  private toast = inject(ToastService);

  loading = signal(false);
  error = signal('');
  forbidden = signal(false);
  logs = signal<IntegrationLogDto[]>([]);

  pageSize = 10;
  pageIndex = signal(1);
  totalPages = computed(() => Math.max(1, Math.ceil(this.logs().length / this.pageSize)));
  pagedLogs = computed(() => {
    const start = (this.pageIndex() - 1) * this.pageSize;
    return this.logs().slice(start, start + this.pageSize);
  });

  statusFilter = signal<StatusFilter>('All');
  targetSystemFilter = signal<string>('');

  reprocessingIds = signal<ReadonlySet<number>>(new Set());

  failedCount = computed(() => this.logs().filter((log) => log.status === 'Failed').length);
  pendingCount = computed(() => this.logs().filter((log) => log.status === 'Pending').length);
  successCount = computed(() => this.logs().filter((log) => log.status === 'Success').length);

  /** Row selected for the details modal. */
  selectedLog = signal<IntegrationLogDto | null>(null);
  private datePipe = new DatePipe('en-US');

  detailRows(log: IntegrationLogDto): DetailRow[] {
    return [
      { label: 'Target system', value: log.targetSystem },
      { label: 'Analysis', value: '#' + log.analysisId },
      { label: 'Status', value: log.status },
      { label: 'Attempted', value: this.datePipe.transform(log.attemptedAtUtc, 'medium') },
      { label: 'Completed', value: log.completedAtUtc ? this.datePipe.transform(log.completedAtUtc, 'medium') : '-' },
      { label: 'Retries', value: log.retryCount },
      { label: 'Error', value: log.errorMessage, full: true, pre: true },
    ];
  }

  ngOnInit(): void {
    this.loadLogs();
  }

  isLabCoordinator(): boolean {
    return this.currentUserService.user()?.role === 'LabCoordinator';
  }

  isReprocessing(id: number): boolean {
    return this.reprocessingIds().has(id);
  }

  onStatusFilterChange(value: string | string[]): void {
    const selected = Array.isArray(value) ? value[0] : value;
    this.statusFilter.set((selected as StatusFilter) || 'All');
    this.loadLogs();
  }

  onTargetSystemFilterChange(value: string | string[]): void {
    this.targetSystemFilter.set(Array.isArray(value) ? value[0] ?? '' : value);
    this.loadLogs();
  }

  reload(): void {
    this.loadLogs();
  }

  reprocess(log: IntegrationLogDto): void {
    if (this.isReprocessing(log.id)) {
      return;
    }
    this.reprocessingIds.update((ids) => new Set(ids).add(log.id));
    this.apiService.reprocess(log.id).subscribe({
      next: () => {
        this.reprocessingIds.update((ids) => {
          const next = new Set(ids);
          next.delete(log.id);
          return next;
        });
        this.toast.success(`Reprocess attempted for #${log.analysisId}.`);
        this.loadLogs();
      },
      error: (err) => {
        this.reprocessingIds.update((ids) => {
          const next = new Set(ids);
          next.delete(log.id);
          return next;
        });
        if (err.status === 400) {
          this.toast.error(
            err.error?.detail || `Reprocessing not supported for ${log.targetSystem}.`
          );
        } else {
          this.toast.error('Failed to reprocess. Please try again.');
        }
      },
    });
  }

  private loadLogs(): void {
    this.loading.set(true);
    this.error.set('');
    this.forbidden.set(false);
    const status = this.statusFilter() === 'All' ? undefined : this.statusFilter();
    const targetSystem = this.targetSystemFilter() || undefined;
    this.apiService.listLogs({ status, targetSystem }).subscribe({
      next: (data) => {
        this.logs.set(data.map((log) => ({ ...log, id: Number(log.id), analysisId: Number(log.analysisId), retryCount: Number(log.retryCount) })));
        this.pageIndex.set(1);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 403) {
          this.forbidden.set(true);
        } else {
          this.error.set('Failed to load integration logs. Please try again.');
        }
      },
    });
  }
}

import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AuditLogDto,
  AuditTrailApiService,
} from './services/audit-trail-api.service';
import { ZardTableImports } from '@/shared/components/table';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import {
  ZardCardComponent,
  ZardCardContentComponent,
} from '@/shared/components/card/card.component';
import { ZardPaginationComponent } from '@/shared/components/pagination/pagination.component';
import { ZardSelectComponent } from '@/shared/components/select/select.component';
import { ZardSelectItemComponent } from '@/shared/components/select/select-item.component';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideAlertCircle, lucideLock, lucideHistory } from '@ng-icons/lucide';

const ENTITY_TYPES = ['Sample', 'Analysis', 'Result', 'Schedule', 'Instrument', 'Template', 'User'];
const ACTIONS = ['Create', 'Update', 'Delete', 'Unlock', 'Login'];

@Component({
  selector: 'lims-audit-trail',
  standalone: true,
  imports: [
    CommonModule,
    ...ZardTableImports,
    ZardSpinnerComponent,
    ZardAlertComponent,
    ZardEmptyComponent,
    ZardCardComponent,
    ZardCardContentComponent,
    ZardPaginationComponent,
    ZardSelectComponent,
    ZardSelectItemComponent,
    ZardButtonComponent,
    NgIcon,
  ],
  templateUrl: './audit-trail.component.html',
  viewProviders: [provideIcons({ lucideAlertCircle, lucideLock, lucideHistory })],
})
export class AuditTrailComponent {
  private apiService = inject(AuditTrailApiService);

  readonly entityTypes = ENTITY_TYPES;
  readonly actions = ACTIONS;

  loading = signal(false);
  error = signal('');
  forbidden = signal(false);
  items = signal<AuditLogDto[]>([]);
  total = signal(0);

  page = signal(1);
  pageSize = 25;
  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize)));

  entityTypeFilter = signal('');
  actionFilter = signal('');

  ngOnInit(): void {
    this.loadAuditLogs();
  }

  private loadAuditLogs(): void {
    this.loading.set(true);
    this.error.set('');
    this.forbidden.set(false);
    this.apiService
      .listAuditLogs({
        entityType: this.entityTypeFilter() || undefined,
        action: this.actionFilter() || undefined,
        page: this.page(),
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (data) => {
          this.items.set(data.items ?? []);
          this.total.set(Number(data.total) || 0);
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          if (err.status === 403) {
            this.forbidden.set(true);
          } else {
            this.error.set('Failed to load audit trail. Please try again.');
          }
        },
      });
  }

  reload(): void {
    this.loadAuditLogs();
  }

  onEntityTypeChange(value: string | string[]): void {
    this.entityTypeFilter.set(Array.isArray(value) ? value[0] ?? '' : value);
    this.page.set(1);
    this.loadAuditLogs();
  }

  onActionChange(value: string | string[]): void {
    this.actionFilter.set(Array.isArray(value) ? value[0] ?? '' : value);
    this.page.set(1);
    this.loadAuditLogs();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.loadAuditLogs();
  }

  entityLabel(log: AuditLogDto): string {
    return `${log.entityType} #${Number(log.entityId)}`;
  }

  changesSummary(log: AuditLogDto): string {
    if (!log.beforeValues && !log.afterValues) {
      return '-';
    }
    return `${log.beforeValues ?? '-'} -> ${log.afterValues ?? '-'}`;
  }
}

import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardCardComponent, ZardCardHeaderComponent, ZardCardContentComponent, ZardCardTitleComponent } from '@/shared/components/card/card.component';
import { ZardTableImports } from '@/shared/components/table/table.imports';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import { StatusBadgeComponent } from '@/shared/ui/status-badge/status-badge.component';
import { AdherenceBadgeComponent } from '@/shared/ui/adherence-badge/adherence-badge.component';
import { ViewToggleComponent, ViewMode } from '@/shared/ui/view-toggle/view-toggle.component';
import { WorkQueueApiService } from './services/work-queue-api.service';
import { CurrentUserService } from '@/shared/services/auth/current-user.service';
import { SearchResultItemDto } from '../../shared/generated/models/search-result-item-dto';
import { AdherenceSummary, ScheduleAdherenceItem } from './services/schedule-adherence.models';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideRefreshCw,
  lucidePlus,
  lucideCircleCheck,
  lucideClock,
  lucideTriangleAlert,
  lucideCircleAlert,
} from '@ng-icons/lucide';

const EMPTY_SUMMARY: AdherenceSummary = { onTrack: 0, due: 0, overdue: 0, missed: 0, total: 0 };

@Component({
  selector: 'lims-work-queue',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ZardButtonComponent,
    ZardCardComponent,
    ZardCardHeaderComponent,
    ZardCardContentComponent,
    ZardCardTitleComponent,
    ZardEmptyComponent,
    ...ZardTableImports,
    StatusBadgeComponent,
    AdherenceBadgeComponent,
    ViewToggleComponent,
    NgIcon,
  ],
  templateUrl: './work-queue.component.html',
  styleUrl: './work-queue.component.scss',
  viewProviders: [
    provideIcons({
      lucideRefreshCw,
      lucidePlus,
      lucideCircleCheck,
      lucideClock,
      lucideTriangleAlert,
      lucideCircleAlert,
    }),
  ],
})
export class WorkQueueComponent {
  private apiService = inject(WorkQueueApiService);
  private currentUserService = inject(CurrentUserService);

  /** Kanban vs flat-list view, remembered per browser. Kanban is the default. */
  viewMode = signal<ViewMode>(this.loadViewMode());
  private persistViewMode = effect(() => {
    try {
      localStorage.setItem('lims-workqueue-view', this.viewMode());
    } catch {
      // storage unavailable; keep the in-memory choice
    }
  });
  private loadViewMode(): ViewMode {
    try {
      return localStorage.getItem('lims-workqueue-view') === 'list' ? 'list' : 'card';
    } catch {
      return 'card';
    }
  }

  /** Only analysts start ad-hoc analyses (they also capture the readings). */
  isAnalyst = computed(() => this.currentUserService.user()?.role === 'ControlLabAnalyst');

  loading = signal(false);
  error = signal('');
  items = signal<SearchResultItemDto[]>([]);

  adherenceLoading = signal(false);
  adherenceError = signal('');
  adherence = signal<ScheduleAdherenceItem[]>([]);
  adherenceSummary = signal<AdherenceSummary>(EMPTY_SUMMARY);

  /** Schedules needing a look - everything that isn't on track. */
  attentionSchedules = computed(() => this.adherence().filter((s) => s.status !== 'OnTrack'));

  columns = computed(() => {
    const allItems = this.items();
    return [
      {
        title: 'Not Started',
        items: allItems.filter((item) => item.status === 'NotStarted'),
      },
      {
        title: 'In Progress',
        items: allItems.filter(
          (item) => item.status === 'InProgress' || item.status === 'OnHold'
        ),
      },
      {
        title: 'Completed',
        items: allItems.filter((item) => item.status === 'Completed'),
      },
    ];
  });

  ngOnInit(): void {
    this.loadWorkQueue();
    this.loadAdherence();
  }

  private loadWorkQueue(): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.getWorkQueue().subscribe({
      next: (data) => {
        this.items.set(data.items || []);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load work queue. Please try again.');
      },
    });
  }

  private loadAdherence(): void {
    this.adherenceLoading.set(true);
    this.adherenceError.set('');
    this.apiService.getAdherence().subscribe({
      next: (data) => {
        this.adherence.set(data.schedules || []);
        this.adherenceSummary.set(data.summary || EMPTY_SUMMARY);
        this.adherenceLoading.set(false);
      },
      error: () => {
        this.adherenceLoading.set(false);
        this.adherenceError.set('Failed to load schedule adherence. Please try again.');
      },
    });
  }

  reload(): void {
    this.loadWorkQueue();
    this.loadAdherence();
  }

  reloadAdherence(): void {
    this.loadAdherence();
  }
}

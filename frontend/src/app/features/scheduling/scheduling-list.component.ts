import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardCardComponent, ZardCardContentComponent } from '@/shared/components/card';
import { ZardBadgeComponent } from '@/shared/components/badge';
import { ZardSpinnerComponent } from '@/shared/components/spinner';
import { ZardTableImports } from '@/shared/components/table';
import { ZardPaginationComponent } from '@/shared/components/pagination/pagination.component';
import { SchedulingApiService } from './services/scheduling-api.service';
import { ScheduleDto } from '../../shared/generated/models/schedule-dto';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { ZardDialogService } from '@/shared/components/dialog/dialog.service';
import { DetailDialogComponent, DetailRow } from '@/shared/ui/detail-dialog/detail-dialog.component';
import { ViewToggleComponent, ViewMode } from '@/shared/ui/view-toggle/view-toggle.component';
import { ToastService } from '@/shared/services/toast/toast.service';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideRefreshCw, lucidePencil, lucideTrash2 } from '@ng-icons/lucide';

@Component({
  selector: 'lims-scheduling-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    ZardButtonComponent,
    ZardCardComponent,
    ZardCardContentComponent,
    ZardBadgeComponent,
    ZardSpinnerComponent,
    ZardPaginationComponent,
    ...ZardTableImports,
    DetailDialogComponent,
    ViewToggleComponent,
    NgIcon,
  ],
  templateUrl: './scheduling-list.component.html',
  styleUrl: './scheduling-list.component.scss',
  viewProviders: [provideIcons({ lucidePlus, lucideRefreshCw, lucidePencil, lucideTrash2 })],
})
export class SchedulingListComponent {
  private apiService = inject(SchedulingApiService);
  private currentUserService = inject(CurrentUserService);
  private dialog = inject(ZardDialogService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loading = signal(false);
  error = signal('');
  schedules = signal<ScheduleDto[]>([]);
  deleting = signal(false);

  pageSize = 10;
  pageIndex = signal(1);
  totalPages = computed(() => Math.max(1, Math.ceil(this.schedules().length / this.pageSize)));
  pagedSchedules = computed(() => {
    const start = (this.pageIndex() - 1) * this.pageSize;
    return this.schedules().slice(start, start + this.pageSize);
  });

  /** List vs card view, remembered per browser. */
  viewMode = signal<ViewMode>(this.loadViewMode());
  private persistViewMode = effect(() => {
    try {
      localStorage.setItem('lims-schedules-view', this.viewMode());
    } catch {
      // storage unavailable; keep the in-memory choice
    }
  });
  private loadViewMode(): ViewMode {
    try {
      return localStorage.getItem('lims-schedules-view') === 'card' ? 'card' : 'list';
    } catch {
      return 'list';
    }
  }

  /** Row selected for the details modal. */
  selectedSchedule = signal<ScheduleDto | null>(null);

  detailRows(schedule: ScheduleDto): DetailRow[] {
    return [
      { label: 'Name', value: schedule.name },
      { label: 'Site', value: schedule.site },
      { label: 'Analysis Type', value: schedule.analysisType },
      { label: 'Shift Pattern', value: schedule.shiftPattern },
      { label: 'Recurrence', value: schedule.recurrencePattern },
      { label: 'Exclusion rules', value: schedule.exclusionRules, full: true },
      { label: 'Assigned', value: schedule.assignedToUserId ? 'User #' + schedule.assignedToUserId : 'Unassigned' },
    ];
  }

  editSchedule(schedule: ScheduleDto): void {
    this.router.navigate(['.', schedule.id, 'edit'], { relativeTo: this.route });
  }

  ngOnInit(): void {
    this.loadSchedules();
  }

  private loadSchedules(): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.listSchedules().subscribe({
      next: (data) => {
        this.schedules.set(data);
        this.pageIndex.set(1);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load schedules. Please try again.');
      },
    });
  }

  reload(): void {
    this.loadSchedules();
  }

  isLabCoordinator(): boolean {
    return this.currentUserService.user()?.role === 'LabCoordinator';
  }

  delete(schedule: ScheduleDto): void {
    this.dialog.create({
      zTitle: `Delete "${schedule.name}"?`,
      zDescription: 'This permanently removes the schedule. This cannot be undone.',
      zOkText: 'Delete schedule',
      zOkDestructive: true,
      zCancelText: 'Cancel',
      zOnOk: () => {
        this.deleting.set(true);
        this.apiService.deleteSchedule(Number(schedule.id)).subscribe({
          next: () => {
            this.deleting.set(false);
            this.toast.success(`Schedule "${schedule.name}" deleted.`);
            this.loadSchedules();
          },
          error: () => {
            this.deleting.set(false);
            this.toast.error(`Could not delete "${schedule.name}". Please try again.`);
          },
        });
      },
    });
  }
}

import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
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
import { ToastService } from '@/shared/services/toast/toast.service';

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
  ],
  templateUrl: './scheduling-list.component.html',
  styleUrl: './scheduling-list.component.scss',
})
export class SchedulingListComponent {
  private apiService = inject(SchedulingApiService);
  private currentUserService = inject(CurrentUserService);
  private dialog = inject(ZardDialogService);
  private toast = inject(ToastService);

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

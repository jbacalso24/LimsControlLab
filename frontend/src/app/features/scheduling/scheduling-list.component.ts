import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ZardButtonComponent } from '@/shared/components/button';
import { ZardCardComponent, ZardCardContentComponent } from '@/shared/components/card';
import { ZardBadgeComponent } from '@/shared/components/badge';
import { ZardSpinnerComponent } from '@/shared/components/spinner';
import { ZardEmptyComponent } from '@/shared/components/empty';
import { ZardTableImports } from '@/shared/components/table';
import { SchedulingApiService } from './services/scheduling-api.service';
import { ScheduleDto } from '../../shared/generated/models/schedule-dto';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';

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
    ZardEmptyComponent,
    ...ZardTableImports,
  ],
  templateUrl: './scheduling-list.component.html',
  styleUrl: './scheduling-list.component.scss',
})
export class SchedulingListComponent {
  private apiService = inject(SchedulingApiService);
  private currentUserService = inject(CurrentUserService);

  loading = signal(false);
  error = signal('');
  schedules = signal<ScheduleDto[]>([]);
  deleting = signal(false);

  ngOnInit(): void {
    this.loadSchedules();
  }

  private loadSchedules(): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.listSchedules().subscribe({
      next: (data) => {
        this.schedules.set(data);
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
    if (!confirm(`Are you sure you want to delete "${schedule.name}"?`)) {
      return;
    }

    this.deleting.set(true);
    this.apiService.deleteSchedule(Number(schedule.id)).subscribe({
      next: () => {
        this.deleting.set(false);
        this.loadSchedules();
      },
      error: () => {
        this.deleting.set(false);
        this.error.set('Failed to delete schedule. Please try again.');
      },
    });
  }
}

import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { WorkQueueApiService } from './services/work-queue-api.service';
import { SearchResultItemDto } from '../../shared/generated/models/search-result-item-dto';

@Component({
  selector: 'lims-work-queue',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonsModule],
  templateUrl: './work-queue.component.html',
  styleUrl: './work-queue.component.scss',
})
export class WorkQueueComponent {
  private apiService = inject(WorkQueueApiService);

  loading = signal(false);
  error = signal('');
  items = signal<SearchResultItemDto[]>([]);

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

  reload(): void {
    this.loadWorkQueue();
  }
}

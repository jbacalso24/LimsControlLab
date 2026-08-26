import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { HistorySearchApiService } from './services/history-search-api.service';
import { SearchResultsRequest } from '../../shared/generated/models/search-results-request';
import { SearchResultItemDto } from '../../shared/generated/models/search-result-item-dto';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardInputComponent } from '@/shared/components/input/input.component';
import { ZardCardComponent, ZardCardHeaderComponent, ZardCardTitleComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import { ZardPaginationComponent } from '@/shared/components/pagination/pagination.component';
import {
  ZardTableComponent,
  ZardTableHeaderComponent,
  ZardTableBodyComponent,
  ZardTableRowComponent,
  ZardTableHeadComponent,
  ZardTableCellComponent,
} from '@/shared/components/table/table.component';
import { StatusBadgeComponent } from '@/shared/ui/status-badge/status-badge.component';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideAlertCircle } from '@ng-icons/lucide';

@Component({
  selector: 'lims-history-search',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ZardButtonComponent,
    ZardInputComponent,
    ZardCardComponent,
    ZardCardHeaderComponent,
    ZardCardTitleComponent,
    ZardCardContentComponent,
    ZardAlertComponent,
    ZardSpinnerComponent,
    ZardEmptyComponent,
    ZardPaginationComponent,
    ZardTableComponent,
    ZardTableHeaderComponent,
    ZardTableBodyComponent,
    ZardTableRowComponent,
    ZardTableHeadComponent,
    ZardTableCellComponent,
    StatusBadgeComponent,
    NgIcon,
  ],
  viewProviders: [provideIcons({ lucideAlertCircle })],
  templateUrl: './history-search.component.html',
  styleUrl: './history-search.component.scss',
})
export class HistorySearchComponent {
  private apiService = inject(HistorySearchApiService);
  private fb = inject(FormBuilder);

  filterForm: FormGroup = this.fb.group({
    templateName: [''],
    testId: [''],
    instrumentId: [''],
    sampleIdentifier: [''],
    fromUtc: [''],
    toUtc: [''],
  });

  loading = signal(false);
  error = signal('');
  items = signal<SearchResultItemDto[]>([]);
  totalCount = signal(0);
  searched = signal(false);
  pageSize = 10;
  currentPageIndex = signal(1);

  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize));

  private isInitialized = signal(false);
  private skipPageChangeEffect = signal(false);

  constructor() {
    // Watch for page changes from pagination component (after initial load)
    effect(() => {
      const pageIndex = this.currentPageIndex();
      if (this.isInitialized() && !this.skipPageChangeEffect() && pageIndex > 1) {
        this.onPageChange(pageIndex);
      }
    });
  }

  ngOnInit(): void {
    // Auto-load on init with empty filters
    this.search();
    this.isInitialized.set(true);
  }

  search(): void {
    this.loading.set(true);
    this.error.set('');
    this.skipPageChangeEffect.set(true);
    this.currentPageIndex.set(1);
    this.searched.set(true);

    const request: SearchResultsRequest = this.buildRequest();

    this.apiService.searchResults(request, 1, this.pageSize).subscribe({
      next: (response) => {
        this.items.set(response.items);
        this.totalCount.set(Number(response.totalCount));
        this.loading.set(false);
        this.skipPageChangeEffect.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load search results. Please try again.');
        this.skipPageChangeEffect.set(false);
      },
    });
  }

  onPageChange(pageIndex: number): void {
    this.loading.set(true);
    this.error.set('');
    this.currentPageIndex.set(pageIndex);

    const request: SearchResultsRequest = this.buildRequest();

    this.apiService.searchResults(request, pageIndex, this.pageSize).subscribe({
      next: (response) => {
        this.items.set(response.items);
        this.totalCount.set(Number(response.totalCount));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load page. Please try again.');
      },
    });
  }

  clearFilters(): void {
    this.filterForm.reset();
    this.search();
  }

  private buildRequest(): SearchResultsRequest {
    const formValue = this.filterForm.value;
    const request: SearchResultsRequest = {};

    if (formValue.templateName) request.templateName = formValue.templateName;
    if (formValue.testId) request.testId = formValue.testId;
    if (formValue.instrumentId) request.instrumentId = formValue.instrumentId;
    if (formValue.sampleIdentifier) request.sampleIdentifier = formValue.sampleIdentifier;
    if (formValue.fromUtc) request.fromUtc = formValue.fromUtc;
    if (formValue.toUtc) request.toUtc = formValue.toUtc;

    return request;
  }
}

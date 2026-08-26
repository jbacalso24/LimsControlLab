import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { GridModule, PageChangeEvent } from '@progress/kendo-angular-grid';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { HistorySearchApiService } from './services/history-search-api.service';
import { SearchResultsRequest } from '../../shared/generated/models/search-results-request';
import { SearchResultItemDto } from '../../shared/generated/models/search-result-item-dto';

@Component({
  selector: 'lims-history-search',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonsModule,
    GridModule,
    InputsModule,
    DateInputsModule,
  ],
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

  pageSize = 50;
  skip = signal(0);

  ngOnInit(): void {
    // Auto-load on init with empty filters
    this.search();
  }

  search(): void {
    this.loading.set(true);
    this.error.set('');
    this.skip.set(0);
    this.searched.set(true);

    const request: SearchResultsRequest = this.buildRequest();

    this.apiService.searchResults(request, 1, this.pageSize).subscribe({
      next: (response) => {
        this.items.set(response.items);
        this.totalCount.set(Number(response.totalCount));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load search results. Please try again.');
      },
    });
  }

  onPageChange(event: PageChangeEvent): void {
    const pageNumber = Math.floor(event.skip / this.pageSize) + 1;
    this.loading.set(true);
    this.error.set('');
    this.skip.set(event.skip);

    const request: SearchResultsRequest = this.buildRequest();

    this.apiService.searchResults(request, pageNumber, this.pageSize).subscribe({
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

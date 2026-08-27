import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import axe from 'axe-core';
import { provideRouter } from '@angular/router';
import { WorkQueueComponent } from './work-queue.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { WorkQueueApiService } from './services/work-queue-api.service';
import { PagedResultOfSearchResultItemDto } from '../../shared/generated/models/paged-result-of-search-result-item-dto';
import { of } from 'rxjs';

describe('WorkQueueComponent - Accessibility', () => {
  let fixture: ComponentFixture<WorkQueueComponent>;
  let service: WorkQueueApiService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkQueueComponent, HttpClientTestingModule],
      providers: [WorkQueueApiService, provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkQueueComponent);
    service = TestBed.inject(WorkQueueApiService);
  });

  it('should not have any a11y violations', async () => {
    const mockResponse: PagedResultOfSearchResultItemDto = {
      items: [],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 0,
    };

    vi.spyOn(service, 'getWorkQueue').mockReturnValue(of(mockResponse));

    fixture.componentInstance.ngOnInit();
    fixture.detectChanges();

    const results = await axe.run(fixture.nativeElement, {
      rules: {
        'color-contrast': { enabled: false },
      },
    });

    expect(results.violations).toEqual([]);
  });
});


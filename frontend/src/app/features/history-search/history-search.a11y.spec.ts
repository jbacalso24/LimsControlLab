import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import axe from 'axe-core';
import { HistorySearchComponent } from './history-search.component';

describe('HistorySearchComponent - Accessibility', () => {
  let fixture: ComponentFixture<HistorySearchComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HistorySearchComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(HistorySearchComponent);
    fixture.detectChanges();
  });

  it('should not have any a11y violations', async () => {
    const results = await axe.run(fixture.nativeElement, {
      rules: {
        'color-contrast': { enabled: false },
      },
    });

    expect(results.violations).toEqual([]);
  });
});



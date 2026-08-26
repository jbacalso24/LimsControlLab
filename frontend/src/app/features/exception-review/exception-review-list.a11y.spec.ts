import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import axe from 'axe-core';
import { ExceptionReviewListComponent } from './exception-review-list.component';

describe('ExceptionReviewListComponent - Accessibility', () => {
  let fixture: ComponentFixture<ExceptionReviewListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExceptionReviewListComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ExceptionReviewListComponent);
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



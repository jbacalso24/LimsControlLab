import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import axe from 'axe-core';
import { WorkQueueComponent } from './work-queue.component';

describe('WorkQueueComponent - Accessibility', () => {
  let fixture: ComponentFixture<WorkQueueComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkQueueComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkQueueComponent);
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



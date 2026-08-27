import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import { provideRouter } from '@angular/router';
import axe from 'axe-core';
import { SchedulingListComponent } from './scheduling-list.component';

describe('SchedulingListComponent - Accessibility', () => {
  let fixture: ComponentFixture<SchedulingListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SchedulingListComponent],
      providers: [provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(SchedulingListComponent);
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



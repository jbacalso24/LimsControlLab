import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import { ActivatedRoute } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import axe from 'axe-core';
import { SchedulingFormComponent } from './scheduling-form.component';

describe('SchedulingFormComponent - Accessibility', () => {
  let fixture: ComponentFixture<SchedulingFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SchedulingFormComponent],
      providers: [
        provideAnimationsAsync(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: (key: string) => (key === 'id' ? null : null) },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SchedulingFormComponent);
    fixture.detectChanges();
  });

  it('should not have any a11y violations', async () => {
    const results = await axe.run(fixture.nativeElement, {
      rules: {
        'color-contrast': { enabled: false },
      },
    });

    expect(results.violations).toEqual([]);
  }, 15000);
});



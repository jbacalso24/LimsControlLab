import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import { ActivatedRoute } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import axe from 'axe-core';
import { TemplatesFormComponent } from './templates-form.component';

describe('TemplatesFormComponent - Accessibility', () => {
  let fixture: ComponentFixture<TemplatesFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TemplatesFormComponent, HttpClientTestingModule],
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

    fixture = TestBed.createComponent(TemplatesFormComponent);
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



import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import axe from 'axe-core';
import { TemplatesListComponent } from './templates-list.component';

describe('TemplatesListComponent - Accessibility', () => {
  let fixture: ComponentFixture<TemplatesListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TemplatesListComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TemplatesListComponent);
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



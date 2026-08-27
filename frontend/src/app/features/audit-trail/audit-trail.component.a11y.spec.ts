import { TestBed, ComponentFixture } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import axe from 'axe-core';
import { AuditTrailComponent } from './audit-trail.component';

describe('AuditTrailComponent - Accessibility', () => {
  let fixture: ComponentFixture<AuditTrailComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AuditTrailComponent],
      providers: [provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(AuditTrailComponent);
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

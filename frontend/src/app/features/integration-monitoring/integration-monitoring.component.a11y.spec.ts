import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import axe from 'axe-core';
import { IntegrationMonitoringComponent } from './integration-monitoring.component';

describe('IntegrationMonitoringComponent - Accessibility', () => {
  let fixture: ComponentFixture<IntegrationMonitoringComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IntegrationMonitoringComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(IntegrationMonitoringComponent);
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

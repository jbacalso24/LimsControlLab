import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { AdherenceBadgeComponent } from './adherence-badge.component';
import { ScheduleAdherenceStatus } from '@/features/scheduling/services/schedule-adherence.models';

describe('AdherenceBadgeComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AdherenceBadgeComponent],
    });
  });

  const cases: Array<{ status: ScheduleAdherenceStatus; label: string }> = [
    { status: 'OnTrack', label: 'On track' },
    { status: 'Due', label: 'Due' },
    { status: 'Overdue', label: 'Overdue' },
    { status: 'Missed', label: 'Missed' },
  ];

  cases.forEach(({ status, label }) => {
    it(`renders the "${label}" label for status ${status}`, () => {
      const fixture = TestBed.createComponent(AdherenceBadgeComponent);
      fixture.componentRef.setInput('status', status);
      fixture.detectChanges();

      expect((fixture.nativeElement as HTMLElement).textContent).toContain(label);
    });
  });
});

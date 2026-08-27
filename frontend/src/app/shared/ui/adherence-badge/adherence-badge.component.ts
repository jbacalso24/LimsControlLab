import { Component, computed, input } from '@angular/core';
import { ZardBadgeComponent } from '@/shared/components/badge/badge.component';
import { ScheduleAdherenceStatus } from '@/features/scheduling/services/schedule-adherence.models';

/**
 * Shared schedule-adherence status pill (BRD 6.2). Maps OnTrack / Due / Overdue / Missed
 * to the design system's status-token recipe so every schedule adherence list renders
 * state identically.
 */
@Component({
  selector: 'lims-adherence-badge',
  standalone: true,
  imports: [ZardBadgeComponent],
  template: `
    <z-badge
      [class]="
        pill() +
        ' inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium'
      "
    >
      <span class="h-1.5 w-1.5 rounded-full bg-current"></span>
      <span>{{ label() }}</span>
    </z-badge>
  `,
})
export class AdherenceBadgeComponent {
  readonly status = input.required<ScheduleAdherenceStatus>();

  private readonly map: Record<ScheduleAdherenceStatus, { tone: string; label: string }> = {
    OnTrack: { tone: 'bg-success/12 text-success border border-success/25', label: 'On track' },
    Due: { tone: 'bg-info/12 text-info border border-info/25', label: 'Due' },
    Overdue: { tone: 'bg-warning/15 text-warning-foreground dark:text-warning border border-warning/30', label: 'Overdue' },
    Missed: { tone: 'bg-destructive/12 text-destructive border border-destructive/25', label: 'Missed' },
  };

  readonly pill = computed(
    () => this.map[this.status()]?.tone ?? 'bg-muted text-muted-foreground border border-border'
  );
  readonly label = computed(() => this.map[this.status()]?.label ?? this.status());
}

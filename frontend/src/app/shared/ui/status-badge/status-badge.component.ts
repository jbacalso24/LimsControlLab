import { Component, computed, input } from '@angular/core';
import { ZardBadgeComponent } from '@/shared/components/badge/badge.component';

/**
 * Shared lifecycle-status pill. One place that maps an analysis/sample lifecycle
 * status (NotStarted / InProgress / OnHold / Completed / Cancelled) to the design
 * system's status-token recipe, so every list and detail screen renders state
 * identically. Registered in ship/shared-helpers-inventory.md.
 */
@Component({
  selector: 'lims-status-badge',
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
export class StatusBadgeComponent {
  readonly status = input.required<string>();

  private readonly map: Record<string, { tone: string; label: string }> = {
    Completed: { tone: 'bg-success/12 text-success border border-success/25', label: 'Completed' },
    InProgress: { tone: 'bg-info/12 text-info border border-info/25', label: 'In Progress' },
    NotStarted: { tone: 'bg-muted text-muted-foreground border border-border', label: 'Not Started' },
    OnHold: { tone: 'bg-warning/15 text-warning-foreground dark:text-warning border border-warning/30', label: 'On Hold' },
    Cancelled: { tone: 'bg-destructive/12 text-destructive border border-destructive/25', label: 'Cancelled' },
  };

  readonly pill = computed(
    () => this.map[this.status()]?.tone ?? 'bg-muted text-muted-foreground border border-border'
  );
  readonly label = computed(() => this.map[this.status()]?.label ?? this.status());
}

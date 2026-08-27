import { ChangeDetectionStrategy, Component, model, ViewEncapsulation } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideList, lucideLayoutGrid } from '@ng-icons/lucide';

export type ViewMode = 'list' | 'card';

/**
 * Small segmented control to switch a screen between a table (list) and a card grid.
 * Two-way bound via `mode`; parents persist the choice (e.g. in localStorage).
 */
@Component({
  selector: 'lims-view-toggle',
  standalone: true,
  imports: [NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  viewProviders: [provideIcons({ lucideList, lucideLayoutGrid })],
  template: `
    <div class="inline-flex items-center gap-0.5 rounded-md border border-border bg-muted/60 p-0.5" role="group" aria-label="View mode">
      <button
        type="button"
        class="inline-flex h-7 w-8 items-center justify-center rounded-[5px] text-muted-foreground transition-colors active:scale-95"
        [class.bg-card]="mode() === 'list'"
        [class.text-foreground]="mode() === 'list'"
        [class.shadow-sm]="mode() === 'list'"
        [attr.aria-pressed]="mode() === 'list'"
        aria-label="List view"
        (click)="mode.set('list')"
      >
        <ng-icon name="lucideList" class="h-4 w-4" />
      </button>
      <button
        type="button"
        class="inline-flex h-7 w-8 items-center justify-center rounded-[5px] text-muted-foreground transition-colors active:scale-95"
        [class.bg-card]="mode() === 'card'"
        [class.text-foreground]="mode() === 'card'"
        [class.shadow-sm]="mode() === 'card'"
        [attr.aria-pressed]="mode() === 'card'"
        aria-label="Card view"
        (click)="mode.set('card')"
      >
        <ng-icon name="lucideLayoutGrid" class="h-4 w-4" />
      </button>
    </div>
  `,
})
export class ViewToggleComponent {
  readonly mode = model<ViewMode>('list');
}

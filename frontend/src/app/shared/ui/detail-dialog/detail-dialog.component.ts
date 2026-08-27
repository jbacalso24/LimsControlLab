import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  ViewEncapsulation,
} from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideX } from '@ng-icons/lucide';
import { ZardButtonComponent } from '@/shared/components/button/button.component';

/** One label/value line in the detail dialog. `full` spans both columns; `pre` renders monospaced, wrapped. */
export interface DetailRow {
  label: string;
  value: string | number | null | undefined;
  full?: boolean;
  pre?: boolean;
}

/**
 * Shared read-only details modal. Opened by a parent when a table row is clicked
 * (`@if (selected()) { <lims-detail-dialog ... /> }`). Renders a title, a
 * label/value grid, and a full-width footer with a Close action plus any projected
 * actions ([dialogFooter]). Closes on backdrop click, the X, or Escape.
 */
@Component({
  selector: 'lims-detail-dialog',
  standalone: true,
  imports: [NgIcon, ZardButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  viewProviders: [provideIcons({ lucideX })],
  styles: [
    `
      .detail-dialog-backdrop { animation: dd-fade 140ms ease-out both; }
      .detail-dialog-panel { animation: dd-pop 180ms cubic-bezier(0.23, 1, 0.32, 1) both; }
      @keyframes dd-fade { from { opacity: 0; } }
      @keyframes dd-pop { from { opacity: 0; transform: scale(0.97); } }
      @media (prefers-reduced-motion: reduce) {
        .detail-dialog-backdrop, .detail-dialog-panel { animation: none; }
      }
    `,
  ],
  template: `
    <div
      class="detail-dialog-backdrop fixed inset-0 z-50 grid place-items-center bg-black/50 p-4"
      role="dialog"
      aria-modal="true"
      [attr.aria-label]="title()"
      (click)="closed.emit()"
      (keydown.escape)="closed.emit()"
      tabindex="-1"
    >
      <div
        class="detail-dialog-panel flex max-h-[85vh] w-full max-w-lg flex-col overflow-hidden rounded-lg border border-border bg-card text-card-foreground shadow-lg"
        (click)="$event.stopPropagation()"
      >
        <!-- Header -->
        <div class="flex items-start justify-between gap-4 border-b border-border px-5 py-4">
          <h2 class="text-base font-semibold tracking-tight">{{ title() }}</h2>
          <button
            z-button
            zType="ghost"
            zSize="icon-sm"
            class="-mr-2 -mt-1 shrink-0"
            (click)="closed.emit()"
            aria-label="Close"
          >
            <ng-icon name="lucideX" class="h-4 w-4" />
          </button>
        </div>

        <!-- Body -->
        <div class="flex-1 overflow-y-auto px-5 py-4">
          <dl class="grid grid-cols-1 gap-x-6 gap-y-4 sm:grid-cols-2">
            @for (row of rows(); track row.label) {
              <div [class.sm:col-span-2]="row.full">
                <dt class="text-xs font-medium uppercase tracking-wider text-muted-foreground">{{ row.label }}</dt>
                @if (row.pre) {
                  <dd class="mt-1">
                    <pre class="max-h-48 overflow-auto whitespace-pre-wrap break-words rounded-md bg-muted px-3 py-2 font-mono text-xs text-foreground">{{ displayValue(row.value) }}</pre>
                  </dd>
                } @else {
                  <dd class="mt-1 text-sm font-medium text-foreground break-words">{{ displayValue(row.value) }}</dd>
                }
              </div>
            }
          </dl>
          <ng-content />
        </div>

        <!-- Footer -->
        <div class="flex items-center justify-end gap-3 border-t border-border px-5 py-4">
          <ng-content select="[dialogFooter]" />
          <button z-button zType="outline" (click)="closed.emit()">Close</button>
        </div>
      </div>
    </div>
  `,
})
export class DetailDialogComponent {
  readonly title = input<string>('Details');
  readonly rows = input<DetailRow[]>([]);
  readonly closed = output<void>();

  protected displayValue(value: string | number | null | undefined): string {
    if (value === null || value === undefined || value === '') return '-';
    return String(value);
  }
}

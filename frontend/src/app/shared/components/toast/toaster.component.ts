import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCircleCheck,
  lucideCircleX,
  lucideTriangleAlert,
  lucideInfo,
  lucideX,
} from '@ng-icons/lucide';
import { ToastService, type ToastType } from '../../services/toast/toast.service';

/**
 * Global toast stack. Render once at the app root (<z-toaster/>). Reads the
 * ToastService signal; motion follows Emil's rules — custom ease-out, a snappier
 * exit than enter, hover-to-pause, and reduced-motion fallbacks.
 */
@Component({
  selector: 'z-toaster',
  standalone: true,
  imports: [NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  viewProviders: [
    provideIcons({ lucideCircleCheck, lucideCircleX, lucideTriangleAlert, lucideInfo, lucideX }),
  ],
  template: `
    @for (t of toast.toasts(); track t.id) {
      <div
        class="toast"
        [attr.data-type]="t.type"
        [class.leaving]="t.leaving"
        role="status"
        aria-live="polite"
        (mouseenter)="toast.pause(t.id)"
        (mouseleave)="toast.resume(t.id)"
      >
        <ng-icon class="icon" [name]="iconFor(t.type)" />
        <div class="body">
          @if (t.title) {
            <p class="title">{{ t.title }}</p>
          }
          <p class="msg">{{ t.message }}</p>
        </div>
        <button type="button" class="close" (click)="toast.dismiss(t.id)" aria-label="Dismiss notification">
          <ng-icon name="lucideX" />
        </button>
      </div>
    }
  `,
  styles: [
    `
      :host {
        position: fixed;
        z-index: 100;
        bottom: 1rem;
        right: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.625rem;
        width: 380px;
        max-width: calc(100vw - 2rem);
        pointer-events: none;
      }
      @media (max-width: 640px) {
        :host {
          left: 1rem;
          right: 1rem;
          width: auto;
        }
      }
      .toast {
        pointer-events: auto;
        display: flex;
        align-items: flex-start;
        gap: 0.75rem;
        padding: 0.875rem 1rem;
        border-radius: var(--radius);
        background: var(--popover);
        color: var(--popover-foreground);
        border: 1px solid var(--border);
        box-shadow:
          0 10px 30px -12px rgb(0 0 0 / 0.28),
          0 4px 10px -6px rgb(0 0 0 / 0.14);
        transition:
          opacity 0.2s cubic-bezier(0.23, 1, 0.32, 1),
          transform 0.2s cubic-bezier(0.23, 1, 0.32, 1);
      }
      @starting-style {
        .toast {
          opacity: 0;
          transform: translateY(10px) scale(0.98);
        }
      }
      .toast.leaving {
        opacity: 0;
        transform: translateY(6px) scale(0.98);
        transition-duration: 0.16s;
      }
      .icon {
        margin-top: 1px;
        flex: none;
        font-size: 1.15rem;
      }
      .toast[data-type='success'] .icon {
        color: var(--success);
      }
      .toast[data-type='error'] .icon {
        color: var(--destructive);
      }
      .toast[data-type='warning'] .icon {
        color: var(--warning);
      }
      .toast[data-type='info'] .icon {
        color: var(--info);
      }
      .toast[data-type='default'] .icon {
        color: var(--muted-foreground);
      }
      .body {
        min-width: 0;
        flex: 1;
      }
      .title {
        font-size: 0.875rem;
        font-weight: 600;
        line-height: 1.25rem;
      }
      .msg {
        font-size: 0.875rem;
        line-height: 1.3rem;
        color: var(--muted-foreground);
        overflow-wrap: anywhere;
      }
      .title + .msg {
        margin-top: 0.125rem;
      }
      .close {
        flex: none;
        display: grid;
        place-items: center;
        height: 1.5rem;
        width: 1.5rem;
        margin: -0.25rem -0.25rem 0 0;
        border-radius: calc(var(--radius) - 4px);
        color: var(--muted-foreground);
        transition:
          background-color 0.15s ease,
          color 0.15s ease,
          transform 0.1s ease-out;
      }
      .close:hover {
        background: var(--muted);
        color: var(--foreground);
      }
      .close:active {
        transform: scale(0.9);
      }
      @media (prefers-reduced-motion: reduce) {
        .toast,
        .toast.leaving {
          transition: opacity 0.12s ease;
          transform: none;
        }
        @starting-style {
          .toast {
            transform: none;
          }
        }
      }
    `,
  ],
})
export class ZardToasterComponent {
  protected readonly toast = inject(ToastService);

  protected iconFor(type: ToastType): string {
    switch (type) {
      case 'success':
        return 'lucideCircleCheck';
      case 'error':
        return 'lucideCircleX';
      case 'warning':
        return 'lucideTriangleAlert';
      default:
        return 'lucideInfo';
    }
  }
}

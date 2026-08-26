import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info' | 'default';

export interface ToastOptions {
  /** Optional bold title shown above the message. */
  title?: string;
  /** Auto-dismiss duration in ms. Defaults: errors 6000, everything else 4000. 0 = sticky. */
  duration?: number;
}

export interface Toast {
  id: number;
  type: ToastType;
  title?: string;
  message: string;
  duration: number;
  /** Marked true just before removal so the view can play the exit transition. */
  leaving?: boolean;
}

/**
 * Lightweight, dependency-free toast service (Sonner-inspired, ZardUI-styled).
 * DX goal: inject once, call `toast.success('Saved')` from anywhere. A single
 * <z-toaster/> at the app root renders the stack. Timers pause while the tab is
 * hidden so a backgrounded toast is still seen when the user returns.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  private readonly timers = new Map<number, { handle: ReturnType<typeof setTimeout>; remaining: number; startedAt: number }>();

  readonly toasts = signal<Toast[]>([]);

  constructor() {
    if (typeof document !== 'undefined') {
      document.addEventListener('visibilitychange', () => {
        if (document.hidden) this.pauseAll();
        else this.resumeAll();
      });
    }
  }

  success(message: string, opts?: ToastOptions) {
    return this.show('success', message, opts);
  }
  error(message: string, opts?: ToastOptions) {
    return this.show('error', message, { duration: 6000, ...opts });
  }
  warning(message: string, opts?: ToastOptions) {
    return this.show('warning', message, opts);
  }
  info(message: string, opts?: ToastOptions) {
    return this.show('info', message, opts);
  }

  show(type: ToastType, message: string, opts?: ToastOptions): number {
    const id = ++this.seq;
    const duration = opts?.duration ?? 4000;
    const toast: Toast = { id, type, message, title: opts?.title, duration };
    this.toasts.update((list) => [...list, toast]);
    if (duration > 0) this.arm(id, duration);
    return id;
  }

  dismiss(id: number) {
    this.clearTimer(id);
    // Flag as leaving, then remove after the exit transition completes.
    this.toasts.update((list) => list.map((t) => (t.id === id ? { ...t, leaving: true } : t)));
    setTimeout(() => {
      this.toasts.update((list) => list.filter((t) => t.id !== id));
    }, 180);
  }

  /** Pause a single toast's timer (used on hover). */
  pause(id: number) {
    const timer = this.timers.get(id);
    if (!timer) return;
    clearTimeout(timer.handle);
    timer.remaining -= Date.now() - timer.startedAt;
  }

  /** Resume a single toast's timer (used on hover-out). */
  resume(id: number) {
    const timer = this.timers.get(id);
    if (!timer) return;
    this.arm(id, Math.max(timer.remaining, 400));
  }

  private arm(id: number, ms: number) {
    const handle = setTimeout(() => this.dismiss(id), ms);
    this.timers.set(id, { handle, remaining: ms, startedAt: Date.now() });
  }

  private clearTimer(id: number) {
    const timer = this.timers.get(id);
    if (timer) clearTimeout(timer.handle);
    this.timers.delete(id);
  }

  private pauseAll() {
    for (const id of this.timers.keys()) this.pause(id);
  }
  private resumeAll() {
    for (const id of [...this.timers.keys()]) this.resume(id);
  }
}

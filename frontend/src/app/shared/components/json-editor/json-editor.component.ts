import {
  ChangeDetectionStrategy,
  Component,
  forwardRef,
  input,
  signal,
  ViewEncapsulation,
} from '@angular/core';
import { NG_VALUE_ACCESSOR, type ControlValueAccessor } from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCircleCheck, lucideCircleAlert, lucideBraces } from '@ng-icons/lucide';

/**
 * A code-block style editor for JSON fields. Reactive-forms compatible
 * (ControlValueAccessor). Monospace, live JSON validation with an inline error,
 * and a one-click pretty-print. Dependency-free — no CodeMirror/Monaco.
 */
@Component({
  selector: 'z-json-editor',
  standalone: true,
  imports: [NgIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => ZardJsonEditorComponent), multi: true },
  ],
  viewProviders: [provideIcons({ lucideCircleCheck, lucideCircleAlert, lucideBraces })],
  template: `
    <div class="overflow-hidden rounded-md border" [class.border-destructive]="error()" [class.border-input]="!error()">
      <div class="flex items-center justify-between gap-2 border-b border-border bg-muted/50 px-3 py-1.5">
        <div class="flex items-center gap-1.5 text-xs font-medium text-muted-foreground">
          <ng-icon name="lucideBraces" class="h-3.5 w-3.5" />
          <span>JSON</span>
        </div>
        <div class="flex items-center gap-2">
          @if (value().trim() && !error()) {
            <span class="inline-flex items-center gap-1 text-xs text-success">
              <ng-icon name="lucideCircleCheck" class="h-3.5 w-3.5" />
              Valid
            </span>
          } @else if (error()) {
            <span class="inline-flex items-center gap-1 text-xs text-destructive">
              <ng-icon name="lucideCircleAlert" class="h-3.5 w-3.5" />
              Invalid
            </span>
          }
          <button
            type="button"
            class="rounded px-2 py-0.5 text-xs font-medium text-muted-foreground transition-colors hover:bg-accent hover:text-foreground disabled:pointer-events-none disabled:opacity-50 active:scale-[0.97]"
            (click)="format()"
            [disabled]="disabled() || !!error() || !value().trim()"
          >
            Format
          </button>
        </div>
      </div>
      <textarea
        class="block w-full resize-y bg-transparent px-3 py-2.5 font-mono text-[13px] leading-relaxed text-foreground outline-none placeholder:text-muted-foreground/70 disabled:cursor-not-allowed disabled:opacity-60"
        spellcheck="false"
        autocomplete="off"
        [rows]="rows()"
        [value]="value()"
        [disabled]="disabled()"
        [attr.aria-label]="ariaLabel()"
        [attr.aria-invalid]="!!error()"
        [placeholder]="placeholder()"
        (input)="onInput($any($event.target).value)"
        (blur)="onTouched()"
      ></textarea>
    </div>
    @if (error()) {
      <p class="mt-1 text-xs text-destructive">Invalid JSON: {{ error() }}</p>
    }
  `,
})
export class ZardJsonEditorComponent implements ControlValueAccessor {
  readonly rows = input(6);
  readonly placeholder = input('{ }');
  readonly ariaLabel = input<string>('JSON editor');

  protected readonly value = signal('');
  protected readonly disabled = signal(false);
  protected readonly error = signal<string | null>(null);

  private onChange: (value: string) => void = () => {};
  protected onTouched: () => void = () => {};

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
    this.validate();
  }
  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }

  protected onInput(text: string): void {
    this.value.set(text);
    this.onChange(text);
    this.validate();
  }

  protected format(): void {
    const raw = this.value().trim();
    if (!raw) return;
    try {
      const formatted = JSON.stringify(JSON.parse(raw), null, 2);
      this.value.set(formatted);
      this.onChange(formatted);
      this.error.set(null);
    } catch {
      /* validate() already surfaced the error */
    }
  }

  private validate(): void {
    const raw = this.value().trim();
    if (!raw) {
      this.error.set(null);
      return;
    }
    try {
      JSON.parse(raw);
      this.error.set(null);
    } catch (e) {
      this.error.set((e as Error).message);
    }
  }
}

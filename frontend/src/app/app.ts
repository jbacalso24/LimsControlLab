import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ZardToasterComponent } from './shared/components/toast/toaster.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ZardToasterComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  constructor() {
    // Apply the persisted (or system) theme app-wide at startup so every route —
    // including the login page, which lives outside the authenticated shell —
    // respects dark mode. The shell's toggle keeps writing to the same key.
    if (typeof document !== 'undefined') {
      try {
        const stored = localStorage.getItem('lims-theme');
        const prefersDark =
          typeof window.matchMedia === 'function' &&
          window.matchMedia('(prefers-color-scheme: dark)').matches;
        const dark = stored === 'dark' || (stored !== 'light' && prefersDark);
        document.documentElement.classList.toggle('dark', dark);
      } catch {
        /* storage unavailable (private mode) — fall back to light */
      }
    }
  }
}


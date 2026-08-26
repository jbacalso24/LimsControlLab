import { Component, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterOutlet, Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideListChecks,
  lucideCalendarClock,
  lucideFileText,
  lucideTriangleAlert,
  lucideSearch,
  lucideArrowLeftRight,
  lucideSun,
  lucideMoon,
  lucideMenu,
  lucideX,
  lucideLogOut,
  lucideChevronsUpDown,
  lucideUserRound,
} from '@ng-icons/lucide';
import { ZardButtonComponent } from '../../components/button/button.component';
import { ZardBadgeComponent } from '../../components/badge/badge.component';
import { ZardDropdownImports } from '../../components/dropdown/dropdown.imports';
import { CurrentUserService } from '../../services/auth/current-user.service';

@Component({
  selector: 'lims-authenticated-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    NgIcon,
    ZardButtonComponent,
    ZardBadgeComponent,
    ...ZardDropdownImports,
  ],
  templateUrl: './authenticated.layout.html',
  styleUrl: './authenticated.layout.scss',
  viewProviders: [
    provideIcons({
      lucideListChecks,
      lucideCalendarClock,
      lucideFileText,
      lucideTriangleAlert,
      lucideSearch,
      lucideArrowLeftRight,
      lucideSun,
      lucideMoon,
      lucideMenu,
      lucideX,
      lucideLogOut,
      lucideChevronsUpDown,
      lucideUserRound,
    }),
  ],
})
export class AuthenticatedLayout {
  private currentUserService = inject(CurrentUserService);
  private router = inject(Router);

  isDarkMode = signal(false);
  isSidebarOpen = signal(false);
  collapsed = signal(false);

  navItems = [
    {
      label: 'Work Queue',
      route: '/analysis/work-queue',
      icon: 'lucideListChecks',
    },
    { label: 'Schedules', route: '/analysis/schedules', icon: 'lucideCalendarClock' },
    { label: 'Templates', route: '/analysis/templates', icon: 'lucideFileText' },
    {
      label: 'Exception Review',
      route: '/analysis/exception-review',
      icon: 'lucideTriangleAlert',
    },
    {
      label: 'History Search',
      route: '/analysis/history-search',
      icon: 'lucideSearch',
    },
    {
      label: 'Sample Transfer',
      route: '/analysis/sample-transfer/1',
      icon: 'lucideArrowLeftRight',
    },
  ];

  constructor() {
    // Initialize dark mode from localStorage or system preference
    if (typeof window !== 'undefined') {
      this.collapsed.set(localStorage.getItem('lims-sidebar-collapsed') === 'true');
      const stored = localStorage.getItem('lims-theme');
      if (stored === 'dark') {
        this.isDarkMode.set(true);
        document.documentElement.classList.add('dark');
      } else if (stored === 'light') {
        this.isDarkMode.set(false);
        document.documentElement.classList.remove('dark');
      } else {
        const prefersDark =
          typeof window.matchMedia === 'function' &&
          window.matchMedia('(prefers-color-scheme: dark)').matches;
        this.isDarkMode.set(prefersDark);
        if (prefersDark) {
          document.documentElement.classList.add('dark');
        }
      }
    }

    // Effect to handle dark mode changes
    effect(() => {
      if (typeof window === 'undefined') return;
      const isDark = this.isDarkMode();
      if (isDark) {
        document.documentElement.classList.add('dark');
        localStorage.setItem('lims-theme', 'dark');
      } else {
        document.documentElement.classList.remove('dark');
        localStorage.setItem('lims-theme', 'light');
      }
    });
  }

  get currentUser() {
    return this.currentUserService.user();
  }

  /** Two-letter avatar initials derived from the username (e.g. "invicta_analyst" -> "IA"). */
  get userInitials(): string {
    const name = this.currentUser?.username ?? '';
    const parts = name.split(/[._\s-]+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  logout() {
    this.currentUserService.clearToken();
    this.router.navigate(['/login']);
  }

  toggleDarkMode() {
    this.isDarkMode.update(v => !v);
  }

  toggleSidebar() {
    const desktop =
      typeof window !== 'undefined' &&
      typeof window.matchMedia === 'function' &&
      window.matchMedia('(min-width: 1024px)').matches;
    if (desktop) {
      this.collapsed.update((v) => !v);
      try {
        localStorage.setItem('lims-sidebar-collapsed', String(this.collapsed()));
      } catch {
        // ignore storage write errors (private mode, etc.)
      }
    } else {
      this.isSidebarOpen.update((v) => !v);
    }
  }

  closeSidebar() {
    this.isSidebarOpen.set(false);
  }
}

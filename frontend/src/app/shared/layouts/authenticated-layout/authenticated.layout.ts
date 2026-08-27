import { Component, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterOutlet, Router, NavigationEnd } from '@angular/router';
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
  lucideDatabase,
  lucideChartSpline,
  lucideScrollText,
  lucideRadioTower,
  lucideChartLine,
} from '@ng-icons/lucide';
import { ZardButtonComponent } from '../../components/button/button.component';
import { ZardBadgeComponent } from '../../components/badge/badge.component';
import { ZardDropdownImports } from '../../components/dropdown/dropdown.imports';
import { ZardDialogService } from '../../components/dialog/dialog.service';
import { CurrentUserService } from '../../services/auth/current-user.service';
import { BreadcrumbService, Crumb } from '../../services/breadcrumb/breadcrumb.service';
import { AdminApiService } from '../../services/api/admin-api.service';
import { ToastService } from '../../services/toast/toast.service';
import { environment } from '../../../../environments/environment';

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
      lucideDatabase,
      lucideChartSpline,
      lucideScrollText,
      lucideRadioTower,
      lucideChartLine,
    }),
  ],
})
export class AuthenticatedLayout {
  private currentUserService = inject(CurrentUserService);
  private router = inject(Router);
  private dialog = inject(ZardDialogService);
  private admin = inject(AdminApiService);
  private toast = inject(ToastService);

  /** Reset demo data is only meaningful against a Development API. */
  readonly showResetData = !environment.production;

  isDarkMode = signal(false);
  isSidebarOpen = signal(false);
  collapsed = signal(false);

  /** Sidebar nav grouped into labelled sections (group headers hidden when collapsed). */
  navGroups = [
    {
      label: 'Work',
      items: [
        { label: 'Work Queue', route: '/analysis/work-queue', icon: 'lucideListChecks' },
        { label: 'Schedules', route: '/analysis/schedules', icon: 'lucideCalendarClock' },
        { label: 'Templates', route: '/analysis/templates', icon: 'lucideFileText' },
        { label: 'Sample Transfer', route: '/analysis/sample-transfer', icon: 'lucideArrowLeftRight' },
        { label: 'Calibration Curves', route: '/analysis/calibration-curves', icon: 'lucideChartSpline' },
      ],
    },
    {
      label: 'Data',
      items: [
        { label: 'History Search', route: '/analysis/history-search', icon: 'lucideSearch' },
        { label: 'Result Comparison', route: '/analysis/result-comparison', icon: 'lucideChartLine' },
      ],
    },
    {
      label: 'Oversight',
      items: [
        { label: 'Exception Review', route: '/analysis/exception-review', icon: 'lucideTriangleAlert' },
        { label: 'Integration Monitoring', route: '/analysis/integration-monitoring', icon: 'lucideRadioTower' },
        { label: 'Audit Trail', route: '/analysis/audit-trail', icon: 'lucideScrollText' },
      ],
    },
  ];

  /** Breadcrumb shown in the top bar; route-derived defaults, optionally enriched by detail pages. */
  private breadcrumb = inject(BreadcrumbService);
  readonly crumbs = this.breadcrumb.crumbs;

  private readonly workQueue = '/analysis/work-queue';
  private readonly templatesList = '/analysis/templates';
  private readonly schedulesList = '/analysis/schedules';
  private readonly sampleTransferList = '/analysis/sample-transfer';

  private crumbsForUrl(url: string): Crumb[] {
    const u = url.split('?')[0].split('#')[0];
    if (u.includes('/work-queue')) return [{ label: 'Work Queue' }];
    if (u.includes('/new-analysis')) return [{ label: 'Work Queue', link: this.workQueue }, { label: 'New analysis' }];
    if (u.includes('/schedules/create')) return [{ label: 'Schedules', link: this.schedulesList }, { label: 'New schedule' }];
    if (/\/schedules\/\d+\/edit/.test(u)) return [{ label: 'Schedules', link: this.schedulesList }, { label: 'Edit schedule' }];
    if (u.includes('/schedules')) return [{ label: 'Schedules' }];
    if (u.includes('/templates/create')) return [{ label: 'Templates', link: this.templatesList }, { label: 'New template' }];
    if (/\/templates\/\d+\/edit/.test(u)) return [{ label: 'Templates', link: this.templatesList }, { label: 'Edit template' }];
    if (u.includes('/templates')) return [{ label: 'Templates' }];
    if (u.includes('/calibration-curves')) return [{ label: 'Calibration Curves' }];
    if (u.includes('/exception-review')) return [{ label: 'Exception Review' }];
    if (u.includes('/history-search')) return [{ label: 'History Search' }];
    if (u.includes('/result-comparison')) return [{ label: 'Result Comparison' }];
    if (/\/sample-transfer\/\d+/.test(u)) return [{ label: 'Sample Transfer', link: this.sampleTransferList }, { label: 'Sample' }];
    if (u.includes('/sample-transfer')) return [{ label: 'Sample Transfer' }];
    if (u.includes('/integration-monitoring')) return [{ label: 'Integration Monitoring' }];
    if (u.includes('/audit-trail')) return [{ label: 'Audit Trail' }];
    if (u.includes('/analysis/analysis')) return [{ label: 'Work Queue', link: this.workQueue }, { label: 'Analysis Execution' }];
    return [{ label: 'LIMS Control Lab' }];
  }

  constructor() {
    // Keep the top-bar breadcrumb in sync with the active route (detail pages may enrich it).
    this.breadcrumb.set(this.crumbsForUrl(this.router.url));
    this.router.events.subscribe((e) => {
      if (e instanceof NavigationEnd) {
        this.breadcrumb.set(this.crumbsForUrl(e.urlAfterRedirects));
      }
    });

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

  /** Dev-only: confirm, then wipe + reseed the illustrative dataset via the API. */
  resetDemoData() {
    this.dialog.create({
      zTitle: 'Reset demo data?',
      zDescription:
        'This wipes all analyses, samples, schedules, results and exceptions, then reseeds the illustrative dataset. This cannot be undone.',
      zOkText: 'Reset data',
      zOkDestructive: true,
      zCancelText: 'Cancel',
      zOnOk: () => {
        const pending = this.toast.show('info', 'Wiping and reseeding the database.', {
          title: 'Resetting demo data',
          duration: 0,
        });
        this.admin.resetData().subscribe({
          next: (r) => {
            this.toast.dismiss(pending);
            this.toast.success(
              `Reseeded ${r.analyses} analyses across ${r.samples} samples. Reloading.`,
              { title: 'Demo data reset' },
            );
            setTimeout(() => window.location.reload(), 1000);
          },
          error: () => {
            this.toast.dismiss(pending);
            this.toast.error('Could not reset demo data. The API must be running in Development.');
          },
        });
      },
    });
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

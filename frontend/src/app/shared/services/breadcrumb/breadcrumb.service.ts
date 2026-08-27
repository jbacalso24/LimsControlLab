import { Injectable, signal } from '@angular/core';

/** One breadcrumb segment. A `link` makes it a routerLink; the last (current) crumb omits it. */
export interface Crumb {
  label: string;
  link?: string;
}

/**
 * Drives the breadcrumb shown in the top bar. The authenticated layout sets route-derived
 * defaults on every navigation; a detail page can override with entity-specific crumbs after
 * its data loads (e.g. templates-form sets ["Templates", "A Massecuite Brix"]).
 */
@Injectable({ providedIn: 'root' })
export class BreadcrumbService {
  readonly crumbs = signal<Crumb[]>([]);

  set(crumbs: Crumb[]): void {
    this.crumbs.set(crumbs);
  }
}

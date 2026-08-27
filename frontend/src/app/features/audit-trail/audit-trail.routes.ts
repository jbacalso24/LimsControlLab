import { Routes } from '@angular/router';

export const auditTrailRoutes: Routes = [
  {
    path: 'audit-trail',
    loadComponent: () =>
      import('./audit-trail.component').then((m) => m.AuditTrailComponent),
  },
];

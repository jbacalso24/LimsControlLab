import { Routes } from '@angular/router';

export const integrationMonitoringRoutes: Routes = [
  {
    path: 'integration-monitoring',
    loadComponent: () =>
      import('./integration-monitoring.component').then((m) => m.IntegrationMonitoringComponent),
  },
];

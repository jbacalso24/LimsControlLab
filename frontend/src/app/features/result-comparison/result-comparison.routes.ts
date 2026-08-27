import { Routes } from '@angular/router';

export const resultComparisonRoutes: Routes = [
  {
    path: 'result-comparison',
    loadComponent: () =>
      import('./result-comparison.component').then((m) => m.ResultComparisonComponent),
  },
];

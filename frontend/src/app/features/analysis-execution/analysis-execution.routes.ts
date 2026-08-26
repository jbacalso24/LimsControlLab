import { Routes } from '@angular/router';

export const analysisExecutionRoutes: Routes = [
  {
    path: 'analysis/:id',
    loadComponent: () =>
      import('./analysis-execution.component').then((m) => m.AnalysisExecutionComponent),
  },
];

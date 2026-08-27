import { Routes } from '@angular/router';

export const analysisExecutionRoutes: Routes = [
  {
    path: 'new-analysis',
    loadComponent: () =>
      import('./new-analysis.component').then((m) => m.NewAnalysisComponent),
  },
  {
    path: 'analysis/:id',
    loadComponent: () =>
      import('./analysis-execution.component').then((m) => m.AnalysisExecutionComponent),
  },
];

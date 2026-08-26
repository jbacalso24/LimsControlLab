import { Routes } from '@angular/router';

export const historySearchRoutes: Routes = [
  {
    path: 'history-search',
    loadComponent: () =>
      import('./history-search.component').then((m) => m.HistorySearchComponent),
  },
];

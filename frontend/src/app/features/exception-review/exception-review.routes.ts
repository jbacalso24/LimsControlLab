import { Routes } from '@angular/router';

export const exceptionReviewRoutes: Routes = [
  {
    path: 'exception-review',
    loadComponent: () =>
      import('./exception-review-list.component').then((m) => m.ExceptionReviewListComponent),
  },
];

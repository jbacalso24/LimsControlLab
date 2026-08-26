import { Routes } from '@angular/router';

export const sampleTransferRoutes: Routes = [
  {
    path: 'sample-transfer',
    loadComponent: () =>
      import('./sample-transfer.component').then((m) => m.SampleTransferComponent),
  },
  {
    path: 'sample-transfer/:id',
    loadComponent: () =>
      import('./sample-transfer.component').then((m) => m.SampleTransferComponent),
  },
];

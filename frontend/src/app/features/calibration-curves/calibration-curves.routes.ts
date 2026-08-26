import { Routes } from '@angular/router';

export const calibrationCurvesRoutes: Routes = [
  {
    path: 'calibration-curves',
    loadComponent: () =>
      import('./calibration-curves.component').then((m) => m.CalibrationCurvesComponent),
  },
];

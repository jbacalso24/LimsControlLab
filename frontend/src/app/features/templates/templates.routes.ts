import { Routes } from '@angular/router';

export const templatesRoutes: Routes = [
  {
    path: 'templates',
    loadComponent: () => import('./templates-list.component').then((m) => m.TemplatesListComponent),
  },
  {
    path: 'templates/create',
    loadComponent: () => import('./templates-form.component').then((m) => m.TemplatesFormComponent),
  },
  {
    path: 'templates/:id/edit',
    loadComponent: () => import('./templates-form.component').then((m) => m.TemplatesFormComponent),
  },
];

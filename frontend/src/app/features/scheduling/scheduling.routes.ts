import { Routes } from '@angular/router';

export const schedulingRoutes: Routes = [
  {
    path: 'schedules',
    loadComponent: () => import('./scheduling-list.component').then((m) => m.SchedulingListComponent),
  },
  {
    path: 'schedules/create',
    loadComponent: () => import('./scheduling-form.component').then((m) => m.SchedulingFormComponent),
  },
  {
    path: 'schedules/:id/edit',
    loadComponent: () => import('./scheduling-form.component').then((m) => m.SchedulingFormComponent),
  },
  {
    path: 'work-queue',
    loadComponent: () => import('./work-queue.component').then((m) => m.WorkQueueComponent),
  },
];

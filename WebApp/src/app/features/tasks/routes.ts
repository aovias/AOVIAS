import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./task-page.component').then((m) => m.TaskPageComponent),
  },
];

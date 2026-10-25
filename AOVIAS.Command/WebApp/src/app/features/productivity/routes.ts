import { Routes } from '@angular/router';

export const PRODUCTIVITY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/my-work.component').then((m) => m.MyWorkComponent),
  },
];

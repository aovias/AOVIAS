import { Routes } from '@angular/router';

export const FRAMEWORKS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/framework-navigator.component').then((m) => m.FrameworkNavigatorComponent),
  },
];

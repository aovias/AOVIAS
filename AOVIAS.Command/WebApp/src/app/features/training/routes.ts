import { Routes } from '@angular/router';

export const TRAINING_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/training-programs-page.component').then((m) => m.TrainingProgramsPageComponent),
  },
  {
    path: 'sessions',
    loadComponent: () =>
      import('./pages/training-sessions-page.component').then((m) => m.TrainingSessionsPageComponent),
  },
  {
    path: 'matrix',
    loadComponent: () =>
      import('./pages/training-matrix-page.component').then((m) => m.TrainingMatrixPageComponent),
  },
];

import { Routes } from '@angular/router';

export const PERSON_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/person-directory.component').then((m) => m.PersonDirectoryComponent),
  },
  {
    path: 'merge',
    loadComponent: () =>
      import('./pages/person-merge-placeholder.component').then((m) => m.PersonMergePlaceholderComponent),
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/person-detail.component').then((m) => m.PersonDetailComponent),
  },
];

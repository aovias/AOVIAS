import { Routes } from '@angular/router';

export const PROFESSIONAL_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./pages/professional-directory.component').then((m) => m.ProfessionalDirectoryComponent) },
  { path: 'renewals', loadComponent: () => import('./pages/renewal-dashboard.component').then((m) => m.RenewalDashboardComponent) },
  { path: ':id/timeline', loadComponent: () => import('./pages/professional-timeline.component').then((m) => m.ProfessionalTimelineComponent) },
  { path: ':id', loadComponent: () => import('./pages/professional-detail.component').then((m) => m.ProfessionalDetailComponent) },
];

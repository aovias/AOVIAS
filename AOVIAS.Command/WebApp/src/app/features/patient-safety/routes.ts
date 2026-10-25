import { Routes } from '@angular/router';

export const PATIENT_SAFETY_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/patient-safety-feature-shell.component').then((m) => m.PatientSafetyFeatureShellComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/patient-safety-dashboard-page.component').then((m) => m.PatientSafetyDashboardPageComponent),
      },
      {
        path: 'command-center',
        loadComponent: () =>
          import('./pages/incident-command-center-page.component').then((m) => m.IncidentCommandCenterPageComponent),
      },
      {
        path: 'investigations',
        loadComponent: () =>
          import('./pages/investigation-workspace-page.component').then((m) => m.InvestigationWorkspacePageComponent),
      },
      {
        path: 'rca',
        loadComponent: () =>
          import('./pages/rca-workspace-page.component').then((m) => m.RcaWorkspacePageComponent),
      },
      {
        path: 'near-misses',
        loadComponent: () =>
          import('./pages/near-misses-page.component').then((m) => m.NearMissesPageComponent),
      },
      {
        path: 'sentinel-events',
        loadComponent: () =>
          import('./pages/sentinel-events-page.component').then((m) => m.SentinelEventsPageComponent),
      },
      {
        path: 'feedback',
        loadComponent: () =>
          import('./pages/patient-feedback-page.component').then((m) => m.PatientFeedbackPageComponent),
      },
      {
        path: 'lessons',
        loadComponent: () =>
          import('./pages/lessons-learned-page.component').then((m) => m.LessonsLearnedPageComponent),
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./pages/patient-safety-analytics-page.component').then((m) => m.PatientSafetyAnalyticsPageComponent),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/patient-safety-settings-page.component').then((m) => m.PatientSafetySettingsPageComponent),
      },
    ],
  },
];

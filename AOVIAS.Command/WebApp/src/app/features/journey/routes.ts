import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/journey-feature-shell.component').then((m) => m.JourneyFeatureShellComponent),
    children: [
      {
        path: '',
        redirectTo: 'execution',
        pathMatch: 'full',
      },
      {
        /** Execution launchpad — compact stage context and remediation entry. */
        path: 'maturity',
        loadComponent: () =>
          import('./pages/journey-maturity-dashboard.component').then(
            (m) => m.JourneyMaturityDashboardComponent,
          ),
      },
      {
        /** Operational mode view — kept for auditors and advanced users. */
        path: 'operational',
        loadComponent: () => import('./pages/journey.component').then((m) => m.JourneyComponent),
      },
      {
        path: 'execution',
        loadComponent: () =>
          import('./pages/journey-execution-shell.component').then((m) => m.JourneyExecutionShellComponent),
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./pages/execution-screen.component').then((m) => m.ExecutionScreenComponent),
          },
          {
            /**
             * Stage-filtered execution view — /app/journey/execution/stage/core (etc.)
             * Pre-sets the maturity stage filter in JourneyWorkspaceStore on init.
             * The ExecutionScreenComponent reads :stage via activated route data (see StageFilterGuard below).
             */
            path: 'stage/:stage',
            loadComponent: () =>
              import('./pages/execution-screen.component').then((m) => m.ExecutionScreenComponent),
          },
          {
            path: 'Progress/:oeGuid',
            loadComponent: () =>
              import('./pages/objective-element-execution-screen.component').then(
                (m) => m.ObjectiveElementExecutionScreenComponent,
              ),
          },
        ],
      },
    ],
  },
];

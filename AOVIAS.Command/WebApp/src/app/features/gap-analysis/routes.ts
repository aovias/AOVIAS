import { Routes } from '@angular/router';

export const GAP_ANALYSIS_ROUTES: Routes = [
  {
    path: '',
    title: 'Gap Analysis Results',
    data: {
      navSection: 'gap-analysis',
      description: 'Assessment-driven gap analysis with recommendations and next actions',
    },
    loadComponent: () =>
      import('./pages/gap-analysis-results.component').then((m) => m.GapAnalysisResultsComponent),
  },
];

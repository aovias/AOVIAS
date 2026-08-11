import { Routes } from '@angular/router';

export const QUALITY_INTELLIGENCE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/quality-intelligence-feature-shell.component').then((m) => m.QualityIntelligenceFeatureShellComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./pages/quality-intelligence-dashboard-page.component').then((m) => m.QualityIntelligenceDashboardPageComponent) },
      { path: 'indicators', loadComponent: () => import('./pages/clinical-indicators-page.component').then((m) => m.ClinicalIndicatorsPageComponent) },
      { path: 'scorecards', loadComponent: () => import('./pages/department-scorecards-page.component').then((m) => m.DepartmentScorecardsPageComponent) },
      { path: 'trends', loadComponent: () => import('./pages/quality-trends-page.component').then((m) => m.QualityTrendsPageComponent) },
      { path: 'benchmarking', loadComponent: () => import('./pages/benchmarking-page.component').then((m) => m.BenchmarkingPageComponent) },
      { path: 'thresholds', loadComponent: () => import('./pages/threshold-management-page.component').then((m) => m.ThresholdManagementPageComponent) },
      { path: 'investigations', loadComponent: () => import('./pages/quality-investigations-page.component').then((m) => m.QualityInvestigationsPageComponent) },
      { path: 'analytics', loadComponent: () => import('./pages/quality-analytics-page.component').then((m) => m.QualityAnalyticsPageComponent) },
      { path: 'executive', loadComponent: () => import('./pages/executive-summary-page.component').then((m) => m.ExecutiveSummaryPageComponent) },
      { path: 'settings', loadComponent: () => import('./pages/quality-settings-page.component').then((m) => m.QualitySettingsPageComponent) },
    ],
  },
];

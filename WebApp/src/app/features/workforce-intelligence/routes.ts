import { Routes } from '@angular/router';
import { permissionGuard } from '../../shared/authorization/permission.guard';

export const WORKFORCE_INTELLIGENCE_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./pages/executive-dashboard.component').then((m) => m.ExecutiveDashboardComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'skills', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.SkillMatrixComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'competencies', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.CompetencyMatrixComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'training', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.TrainingDashboardComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'certifications', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.CertificationDashboardComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'licenses', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.LicenseDashboardComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'availability', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.AvailabilityDashboardComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'organization', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.OrganizationAnalyticsComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'responsibilities', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.ResponsibilityAnalyticsComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'committees', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.CommitteeAnalyticsComponent), canActivate: [permissionGuard('Intelligence.Dashboard.Read')] },
  { path: 'forecast', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.ForecastDashboardComponent), canActivate: [permissionGuard('Intelligence.Forecast.Read')] },
  { path: 'recommendations', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.RecommendationsComponent), canActivate: [permissionGuard('Intelligence.Recommendations.Read')] },
  { path: 'search', loadComponent: () => import('./pages/intelligence-pages.component').then((m) => m.WorkforceSearchComponent), canActivate: [permissionGuard('Intelligence.Search.Read')] },
];

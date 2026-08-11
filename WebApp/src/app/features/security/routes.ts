import { Routes } from '@angular/router';

export const SECURITY_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./pages/security-dashboard.component').then((m) => m.SecurityDashboardComponent) },
  { path: 'devices', loadComponent: () => import('./pages/security-devices.component').then((m) => m.SecurityDevicesComponent) },
  { path: 'sessions', loadComponent: () => import('./pages/security-sessions.component').then((m) => m.SecuritySessionsComponent) },
  { path: 'risk', loadComponent: () => import('./pages/security-risk.component').then((m) => m.SecurityRiskComponent) },
  { path: 'timeline', loadComponent: () => import('./pages/security-timeline.component').then((m) => m.SecurityTimelineComponent) },
  { path: 'policies', loadComponent: () => import('./pages/security-policies.component').then((m) => m.SecurityPoliciesComponent) },
  { path: 'analytics', loadComponent: () => import('./pages/security-analytics.component').then((m) => m.SecurityAnalyticsComponent) },
];

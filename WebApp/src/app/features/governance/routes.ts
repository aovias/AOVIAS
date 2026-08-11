import { Routes } from '@angular/router';

export const GOVERNANCE_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./pages/governance-dashboard.component').then((m) => m.GovernanceDashboardComponent) },
  { path: 'responsibilities', loadComponent: () => import('./pages/responsibility-matrix.component').then((m) => m.ResponsibilityMatrixComponent) },
  { path: 'committees', loadComponent: () => import('./pages/committee-management.component').then((m) => m.CommitteeManagementComponent) },
  { path: 'committees/:id/members', loadComponent: () => import('./pages/committee-membership.component').then((m) => m.CommitteeMembershipComponent) },
  { path: 'committees/:id/meetings', loadComponent: () => import('./pages/committee-meetings.component').then((m) => m.CommitteeMeetingsComponent) },
  { path: 'approvals', loadComponent: () => import('./pages/approval-matrix.component').then((m) => m.ApprovalMatrixComponent) },
  { path: 'escalations', loadComponent: () => import('./pages/escalation-matrix.component').then((m) => m.EscalationMatrixComponent) },
  { path: 'ownership', loadComponent: () => import('./pages/ownership-explorer.component').then((m) => m.OwnershipExplorerComponent) },
  { path: 'timeline', loadComponent: () => import('./pages/governance-timeline.component').then((m) => m.GovernanceTimelineComponent) },
  { path: 'analytics', loadComponent: () => import('./pages/governance-analytics.component').then((m) => m.GovernanceAnalyticsComponent) },
];

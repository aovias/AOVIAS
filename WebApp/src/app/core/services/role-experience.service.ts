import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';
import { BaseApiService } from '../../shared/services/base-api.service';
import { UserContextService } from './user-context.service';
import { normalizeRoleId } from '../auth/roles/role-definitions';

export interface RoleExperiencePriority {
  title: string;
  rationale?: string | null;
  href?: string | null;
  severity?: string | null;
}

export interface RoleExperienceAttentionCounts {
  overdueTasks: number;
  pendingReviews: number;
  criticalCapas: number;
  upcomingAudits: number;
}

export interface RoleExperienceQuickAction {
  label: string;
  href: string;
}

export interface RoleExperienceHome {
  packId: string;
  persona: string;
  defaultHomeRoute: string;
  priorities: RoleExperiencePriority[];
  attentionCounts: RoleExperienceAttentionCounts;
  quickActions: RoleExperienceQuickAction[];
  widgetAllowList: string[];
  navSectionIds: string[];
}

/** Local fallback pack map when `/role-experience/home` is unavailable. */
const LOCAL_PACK_BY_ROLE: Array<{ match: (roles: string[]) => boolean; home: RoleExperienceHome }> =
  [
    {
      match: (roles) => roles.some((r) => ['ORG_ADMIN', 'GROUP_ADMIN', 'SUPER_ADMIN'].includes(r)),
      home: {
        packId: 'hospital-admin',
        persona: 'ExecutiveAdvisor',
        defaultHomeRoute: '/app/dashboard?persona=hospital-admin',
        priorities: [],
        attentionCounts: {
          overdueTasks: 0,
          pendingReviews: 0,
          criticalCapas: 0,
          upcomingAudits: 0,
        },
        quickActions: [
          { label: 'Generate report', href: '/app/reports/readiness' },
          { label: 'Review risks', href: '/app/ccc/risk-escalation' },
          { label: 'Audit calendar', href: '/app/audit/calendar' },
          { label: 'Approvals', href: '/app/governance/approvals' },
          { label: 'Department performance', href: '/app/analytics/departments' },
          { label: 'Maturity', href: '/app/compliance-maturity' },
        ],
        widgetAllowList: [
          'home-executive-kpi-strip',
          'home-alert-banner',
          'home-compliance-gauge',
          'home-hospital-overview',
          'home-framework-overview',
          'home-department-heatmap',
          'home-risk-distribution',
          'home-milestones',
          'home-score-trend',
        ],
        navSectionIds: ['dashboard', 'action-center'],
      },
    },
    {
      match: (roles) =>
        roles.some(
          (r) => /department.?head/i.test(r) || r === 'DEPARTMENT_HEAD' || r === 'DepartmentHead',
        ),
      home: {
        packId: 'department-head',
        persona: 'DepartmentHeadAssistant',
        defaultHomeRoute: '/app/dashboard?persona=department-head',
        priorities: [],
        attentionCounts: {
          overdueTasks: 0,
          pendingReviews: 0,
          criticalCapas: 0,
          upcomingAudits: 0,
        },
        quickActions: [
          { label: 'Department view', href: '/app/dashboard?persona=department-head' },
          { label: 'My tasks', href: '/app/tasks' },
          { label: 'Evidence', href: '/app/evidence' },
        ],
        widgetAllowList: [
          'home-alert-banner',
          'home-department-heatmap',
          'home-risk-distribution',
          'home-milestones',
        ],
        navSectionIds: ['dashboard', 'compliance-execution', 'action-center'],
      },
    },
    {
      match: (roles) => roles.some((r) => ['EXTERNAL_AUDITOR', 'REGULATOR'].includes(r)),
      home: {
        packId: 'auditor',
        persona: 'AuditorAssistant',
        defaultHomeRoute: '/app/auditor/dashboard',
        priorities: [],
        attentionCounts: {
          overdueTasks: 0,
          pendingReviews: 0,
          criticalCapas: 0,
          upcomingAudits: 0,
        },
        quickActions: [
          { label: 'Auditor dashboard', href: '/app/auditor/dashboard' },
          { label: 'Sessions', href: '/app/auditor/sessions' },
          { label: 'Evidence review', href: '/app/auditor/evidence-review' },
        ],
        widgetAllowList: ['home-alert-banner', 'home-compliance-gauge', 'home-risk-distribution'],
        navSectionIds: ['dashboard', 'action-center'],
      },
    },
    {
      match: (roles) =>
        roles.some((r) => ['COMPLIANCE_VIEWER', 'REGULATORY_ADMIN', 'AUTHORITY'].includes(r)),
      home: {
        packId: 'consultant',
        persona: 'ExecutiveAdvisor',
        defaultHomeRoute: '/app/dashboard?persona=consultant',
        priorities: [],
        attentionCounts: {
          overdueTasks: 0,
          pendingReviews: 0,
          criticalCapas: 0,
          upcomingAudits: 0,
        },
        quickActions: [
          { label: 'Portfolio dashboard', href: '/app/dashboard?persona=consultant' },
          { label: 'Readiness', href: '/app/compliance-maturity' },
          { label: 'Reports', href: '/app/reports/readiness' },
        ],
        widgetAllowList: [
          'home-executive-kpi-strip',
          'home-alert-banner',
          'home-compliance-gauge',
          'home-framework-overview',
          'home-milestones',
          'home-risk-distribution',
        ],
        navSectionIds: ['dashboard', 'compliance-execution'],
      },
    },
    {
      match: () => true,
      home: {
        packId: 'quality-manager',
        persona: 'QualityManagerAssistant',
        defaultHomeRoute: '/app/role-homes/qm',
        priorities: [],
        attentionCounts: {
          overdueTasks: 0,
          pendingReviews: 0,
          criticalCapas: 0,
          upcomingAudits: 0,
        },
        quickActions: [
          { label: 'Assign work', href: '/app/tasks' },
          { label: 'Review evidence', href: '/app/evidence/review-queue' },
          { label: 'Launch audit', href: '/app/audit/planning' },
          { label: 'Generate CAPA', href: '/app/audit/capa' },
          { label: 'View trends', href: '/app/analytics' },
          { label: 'Control center', href: '/app/control-center' },
        ],
        widgetAllowList: [
          'home-executive-kpi-strip',
          'home-alert-banner',
          'home-compliance-gauge',
          'home-department-heatmap',
          'home-risk-distribution',
          'home-milestones',
          'home-framework-overview',
        ],
        navSectionIds: ['dashboard', 'compliance-execution', 'action-center'],
      },
    },
  ];

@Injectable({ providedIn: 'root' })
export class RoleExperienceService extends BaseApiService {
  private readonly userContext = inject(UserContextService);

  getHome(frameworkId?: string | null): Observable<RoleExperienceHome> {
    const params: Record<string, string> = {};
    if (frameworkId) params['frameworkId'] = frameworkId;

    return this.get<RoleExperienceHome>('/role-experience/home', params).pipe(
      map((home) => ({
        ...home,
        priorities: home.priorities ?? [],
        quickActions: home.quickActions ?? [],
        widgetAllowList: home.widgetAllowList ?? [],
        navSectionIds: home.navSectionIds ?? [],
        attentionCounts: home.attentionCounts ?? {
          overdueTasks: 0,
          pendingReviews: 0,
          criticalCapas: 0,
          upcomingAudits: 0,
        },
      })),
      catchError(() => of(this.resolveLocalPack())),
    );
  }

  resolveLocalPack(roles?: string[]): RoleExperienceHome {
    const raw = roles ?? this.userContext.getUserRoles();
    const normalized = raw.map((r) => normalizeRoleId(r) ?? r);
    const combined = [...raw, ...normalized];
    for (const entry of LOCAL_PACK_BY_ROLE) {
      if (entry.match(combined)) {
        return { ...entry.home };
      }
    }
    return { ...LOCAL_PACK_BY_ROLE[LOCAL_PACK_BY_ROLE.length - 1].home };
  }

  /** Resolve a post-login route (API when possible, else local pack). */
  resolveDefaultHomeRoute(): Observable<string> {
    return this.getHome().pipe(
      map((h) => h.defaultHomeRoute || '/app/dashboard'),
      catchError(() => of(this.resolveLocalPack().defaultHomeRoute)),
    );
  }
}

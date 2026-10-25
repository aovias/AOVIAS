import { Routes } from '@angular/router';
import { permissionGuard } from '../../shared/authorization/permission.guard';
import { settingsDirtyGuard } from './guards/settings-dirty.guard';

const placeholder = () =>
  import('./pages/settings-placeholder-page.component').then(
    (m) => m.SettingsPlaceholderPageComponent,
  );

/** Access governance pages share one permission; declared once so they cannot drift. */
const accessGovernanceGuard = permissionGuard('Compliance.Standard.View');

/**
 * Enterprise Administration Workspace — Stage 5A.
 *
 * Twelve tabs: organization, programs, templates, frameworks, risk, workflow,
 * notifications, approvals, teams, security, ai, system.
 *
 * Every path that existed before Stage 5A still resolves. Where a page moved tabs, the
 * old path is kept as a redirect rather than deleted — deep links, bookmarks, and the
 * `legacyAdminRedirect` entries in `app.routes.ts` all depend on it.
 */
export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/administration-shell.component').then((m) => m.AdministrationShellComponent),
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/settings-home-page.component').then((m) => m.SettingsHomePageComponent),
      },

      // ─────────────────────────────── Organization ───────────────────────────────
      {
        path: 'organization/profile',
        loadComponent: () =>
          import('./pages/organization-settings-page.component').then(
            (m) => m.OrganizationSettingsPageComponent,
          ),
        canDeactivate: [settingsDirtyGuard],
      },
      {
        path: 'organization/branding',
        loadComponent: () =>
          import('./pages/branding-page.component').then((m) => m.BrandingPageComponent),
        canDeactivate: [settingsDirtyGuard],
      },
      {
        path: 'organization/facilities',
        loadComponent: placeholder,
        data: {
          title: 'Facilities & Locations',
          description:
            'Hospital information, facilities, buildings, units, and locations for the organization.',
          comingSoon: true,
          futureItems: ['Facilities', 'Buildings', 'Units', 'Locations'],
        },
      },
      {
        path: 'organization/financial-year',
        loadComponent: placeholder,
        data: {
          title: 'Financial Year',
          description: 'Financial year definition used by programs, reporting, and retention.',
          comingSoon: true,
        },
      },
      {
        path: 'organization/calendar',
        loadComponent: placeholder,
        data: {
          title: 'Business Calendar',
          description: 'Working days, holidays, and blackout periods used by audit scheduling.',
          comingSoon: true,
        },
      },
      {
        path: 'organization/localization',
        loadComponent: placeholder,
        data: {
          title: 'Localization & Time Zone',
          description: 'Locale, regional settings, and organization time zone.',
          comingSoon: true,
          futureItems: ['Localization', 'Regional Settings', 'Time Zone'],
        },
      },
      { path: 'organization', redirectTo: 'organization/profile', pathMatch: 'full' },
      // Legacy placements (pre–Stage 5A "General" section)
      { path: 'general/organization', redirectTo: 'organization/profile', pathMatch: 'full' },
      { path: 'general/branding', redirectTo: 'organization/branding', pathMatch: 'full' },
      { path: 'general', redirectTo: 'organization/profile', pathMatch: 'full' },

      // ───────────────────────────────── Programs ─────────────────────────────────
      {
        path: 'programs',
        pathMatch: 'full',
        loadComponent: placeholder,
        data: {
          title: 'Audit Programs',
          description:
            'Annual, quarterly, monthly, risk-based, and department audit programs. Schema lands in Stage 5B.',
          comingSoon: true,
          futureItems: [
            'Annual, quarterly, and monthly programs',
            'Risk-based and department programs',
            'Program approval',
            'Coverage tracking and progress monitoring',
            'Automatic audit generation',
          ],
        },
      },
      {
        path: 'programs/schedule',
        loadComponent: placeholder,
        data: {
          title: 'Program Schedule',
          description: 'Audit cadence and generated audit schedule per program.',
          comingSoon: true,
        },
      },
      {
        path: 'programs/coverage',
        loadComponent: placeholder,
        data: {
          title: 'Coverage & Progress',
          description: 'Framework and department coverage against the active programs.',
          comingSoon: true,
        },
      },

      // ───────────────────────────────── Templates ────────────────────────────────
      {
        path: 'templates',
        pathMatch: 'full',
        loadComponent: placeholder,
        data: {
          title: 'Template Library',
          description:
            'Audit, checklist, evidence, finding, CAPA, and report templates. Schema lands in Stage 5B.',
          comingSoon: true,
          futureItems: [
            'Audit and checklist templates',
            'Evidence and finding templates',
            'CAPA and report templates',
            'Versioning, approval, and clone',
            'Import and export',
          ],
        },
      },
      {
        path: 'templates/library',
        loadComponent: placeholder,
        data: {
          title: 'Template Library',
          description: 'Browse and manage templates by artefact type.',
          comingSoon: true,
        },
      },
      {
        path: 'templates/versions',
        loadComponent: placeholder,
        data: {
          title: 'Versioning & Approval',
          description: 'Template versions, approval state, and publication history.',
          comingSoon: true,
        },
      },

      // ──────────────────────────────── Frameworks ────────────────────────────────
      {
        path: 'frameworks',
        pathMatch: 'full',
        loadComponent: () =>
          import('./pages/frameworks-admin-page.component').then(
            (m) => m.FrameworksAdminPageComponent,
          ),
      },
      {
        path: 'frameworks/cross-mapping',
        loadComponent: placeholder,
        data: {
          title: 'Cross Mapping',
          description: 'Map standards and objective elements across frameworks.',
          comingSoon: true,
        },
      },

      // ─────────────────────────── Configuration Center ───────────────────────────
      /**
       * One route for every configuration domain.
       *
       * The domain is a query parameter rather than a path segment because the set of domains is
       * the configuration registry, not the router — adding a catalog must not require a route.
       * The tab entries below deep-link into it with `?category=…`.
       */
      {
        path: 'configuration',
        /**
         * Guarded with the access-governance permission (DEF-RC1-001).
         *
         * This page writes business rules for the whole organization, so it belongs in the most
         * restricted administration cluster — the same one that gates roles, policies, delegations
         * and the approval matrix.
         *
         * A dedicated `Configuration.Manage` code would be more precise, and is the recommended
         * follow-up. It is deliberately *not* used yet: `AuthorizationService.can` fails closed, so
         * a permission code the backend catalog does not contain would lock every administrator out
         * of this page until the two deploy together. Reusing a seeded code protects the route
         * today without coupling the release.
         */
        canActivate: [accessGovernanceGuard],
        loadComponent: () =>
          import('./pages/configuration-center-page.component').then(
            (m) => m.ConfigurationCenterPageComponent,
          ),
      },

      // ─────────────────────────────────── Risk ───────────────────────────────────
      { path: 'risk', pathMatch: 'full', redirectTo: 'configuration?category=RiskLikelihood' },
      { path: 'risk/severity', redirectTo: 'configuration?category=FindingSeverity' },
      { path: 'risk/escalation', redirectTo: 'configuration?category=RiskEscalationThreshold' },

      // ───────────────────────────────── Workflow ─────────────────────────────────
      {
        path: 'workflow',
        pathMatch: 'full',
        loadComponent: placeholder,
        data: {
          title: 'Workflow Lifecycle',
          description:
            'Configurable audit lifecycle. The rule engine already dispatches from a rule collection; Stage 5B adds configuration-backed rules alongside the compiled ones.',
          comingSoon: true,
          futureItems: [
            'Lifecycle and transitions',
            'Validation rules',
            'Parallel and conditional flows',
            'Mandatory and optional activities',
            'Auto progression and versioning',
          ],
        },
      },
      {
        path: 'workflow/transitions',
        loadComponent: placeholder,
        data: {
          title: 'Transitions',
          description: 'Allowed state transitions and their validation rules.',
          comingSoon: true,
        },
      },
      {
        path: 'workflow/versions',
        loadComponent: placeholder,
        data: {
          title: 'Workflow Versions',
          description: 'Published workflow versions and their activation history.',
          comingSoon: true,
        },
      },

      // ─────────────────────────────── Notifications ──────────────────────────────
      {
        path: 'notifications',
        loadChildren: () =>
          import('../notifications/routes').then((m) => m.NOTIFICATION_ADMIN_ROUTES),
      },
      // Legacy placements (pre–Stage 5A "Platform → Notifications"). The renamed targets
      // must precede the prefix redirect, otherwise the prefix match claims them first and
      // sends `delivery` to a non-existent `notifications/delivery`.
      {
        path: 'platform/notifications/delivery',
        redirectTo: 'notifications/queues',
        pathMatch: 'full',
      },
      {
        path: 'platform/notifications/reminders',
        redirectTo: 'notifications/subscriptions',
        pathMatch: 'full',
      },
      { path: 'platform/notifications/campaigns', redirectTo: 'notifications', pathMatch: 'full' },
      {
        path: 'platform/notifications/availability',
        redirectTo: 'notifications/preferences',
        pathMatch: 'full',
      },
      { path: 'platform/notifications', redirectTo: 'notifications', pathMatch: 'prefix' },

      // ───────────────────────────────── Approvals ────────────────────────────────
      {
        path: 'approvals/matrix',
        loadComponent: () =>
          import('../access-governance/pages/approval-matrix.component').then(
            (m) => m.ApprovalMatrixComponent,
          ),
        canActivate: [accessGovernanceGuard],
      },
      {
        path: 'approvals/delegations',
        loadComponent: () =>
          import('../access-governance/pages/delegation-center.component').then(
            (m) => m.DelegationCenterComponent,
          ),
        canActivate: [accessGovernanceGuard],
      },
      {
        path: 'approvals/chains',
        loadComponent: placeholder,
        data: {
          title: 'Approval Chains',
          description:
            'Single-level, multi-level, parallel, and conditional approval chains with escalation.',
          comingSoon: true,
        },
      },
      {
        path: 'approvals/signatures',
        loadComponent: placeholder,
        data: {
          title: 'Digital Signatures',
          description:
            'Electronic signatures, approval certificates, identity verification, timestamps, and non-repudiation.',
          comingSoon: true,
        },
      },
      { path: 'approvals', redirectTo: 'approvals/matrix', pathMatch: 'full' },

      // ─────────────────────────────────── Teams ──────────────────────────────────
      {
        path: 'teams',
        loadChildren: () =>
          import('../workforce-admin/routes').then((m) => m.WORKFORCE_ADMIN_ROUTES),
      },
      // Legacy placement (pre–Stage 5A "Workforce" section)
      { path: 'workforce', redirectTo: 'teams', pathMatch: 'prefix' },

      // ────────────────────────────────── Security ────────────────────────────────
      {
        path: 'security/users',
        loadComponent: () =>
          import('./identity-admin/pages/identity-users-page.component').then(
            (m) => m.IdentityUsersPageComponent,
          ),
        canActivate: [permissionGuard('Users.View')],
      },
      {
        path: 'security/password',
        loadComponent: () =>
          import('./identity-admin/pages/identity-password-management-page.component').then(
            (m) => m.IdentityPasswordManagementPageComponent,
          ),
        canActivate: [permissionGuard('PasswordManagement.Manage')],
      },
      {
        path: 'security/roles',
        loadComponent: () =>
          import('../access-governance/pages/role-management.component').then(
            (m) => m.RoleManagementComponent,
          ),
        canActivate: [accessGovernanceGuard],
      },
      {
        path: 'security/permissions',
        loadComponent: () =>
          import('../access-governance/pages/permission-catalog.component').then(
            (m) => m.PermissionCatalogComponent,
          ),
        canActivate: [accessGovernanceGuard],
      },
      {
        path: 'security/policies',
        loadComponent: () =>
          import('../access-governance/pages/policy-designer.component').then(
            (m) => m.PolicyDesignerComponent,
          ),
        canActivate: [accessGovernanceGuard],
      },
      {
        path: 'security/effective',
        loadComponent: () =>
          import('../access-governance/pages/effective-permissions.component').then(
            (m) => m.EffectivePermissionsComponent,
          ),
        canActivate: [accessGovernanceGuard],
      },
      {
        path: 'security/simulator',
        loadComponent: () =>
          import('../access-governance/pages/authorization-simulator.component').then(
            (m) => m.AuthorizationSimulatorComponent,
          ),
        canActivate: [accessGovernanceGuard],
      },
      {
        path: 'security/posture',
        loadChildren: () => import('../security/routes').then((m) => m.SECURITY_ROUTES),
      },
      { path: 'security', redirectTo: 'security/users', pathMatch: 'full' },
      // Legacy placement (pre–Stage 5A "Identity & Access" section)
      { path: 'identity/users', redirectTo: 'security/users', pathMatch: 'full' },
      { path: 'identity/password', redirectTo: 'security/password', pathMatch: 'full' },

      // ──────────────────────────────────── AI ────────────────────────────────────
      {
        path: 'ai',
        loadChildren: () => import('../ai-identity/routes').then((m) => m.AI_ADMIN_ROUTES),
      },
      // Legacy placements under Identity / old AI nesting → Administration AI
      { path: 'identity/ai-identity', redirectTo: 'ai/dashboard', pathMatch: 'full' },
      { path: 'identity/ai-identity/profiles', redirectTo: 'ai/identity', pathMatch: 'full' },
      { path: 'identity/ai-identity/prompts', redirectTo: 'ai/prompts', pathMatch: 'full' },
      { path: 'identity/ai-identity/prompts/:id/versions', redirectTo: 'ai/prompts/:id/versions' },
      { path: 'identity/ai-identity/policies', redirectTo: 'ai/policies', pathMatch: 'full' },
      { path: 'identity/ai-identity/permissions', redirectTo: 'ai/permissions', pathMatch: 'full' },
      { path: 'identity/ai-identity/executions', redirectTo: 'ai/executions', pathMatch: 'full' },
      { path: 'identity/ai-identity/analytics', redirectTo: 'ai/analytics', pathMatch: 'full' },
      { path: 'identity/ai-identity/approvals', redirectTo: 'ai/approvals', pathMatch: 'full' },

      // ────────────────────────────────── System ──────────────────────────────────
      {
        path: 'system/audit',
        loadChildren: () => import('../admin-audit/routes').then((m) => m.ADMIN_AUDIT_ROUTES),
      },
      {
        path: 'system/integrations',
        loadChildren: () => import('../integration/routes').then((m) => m.INTEGRATION_ROUTES),
      },
      {
        path: 'system/licensing',
        loadComponent: () =>
          import('./pages/licensing-page.component').then((m) => m.LicensingPageComponent),
      },
      {
        path: 'system/commercial',
        loadComponent: () =>
          import('./pages/commercial-ops-page.component').then((m) => m.CommercialOpsPageComponent),
      },
      {
        path: 'system/compliance-defaults',
        loadComponent: placeholder,
        data: {
          title: 'Compliance Defaults',
          description: 'Organization-wide compliance defaults and policy baselines.',
          comingSoon: true,
        },
      },
      {
        path: 'system/number-sequences',
        loadComponent: placeholder,
        data: {
          title: 'Number Sequences',
          description:
            'Document and record numbering used by audits, findings, CAPAs, and reports.',
          comingSoon: true,
        },
      },
      {
        path: 'system/retention',
        loadComponent: placeholder,
        data: {
          title: 'Retention & Storage',
          description: 'Retention periods, storage policies, and backup policies.',
          comingSoon: true,
          futureItems: ['Retention', 'Storage policies', 'Backup policies'],
        },
      },
      {
        path: 'system/feature-flags',
        loadComponent: placeholder,
        data: {
          title: 'Feature Flags',
          description:
            'Platform feature flags. These become the rollback mechanism for each Stage 5C configuration cutover.',
          comingSoon: true,
        },
      },
      { path: 'system', redirectTo: 'system/audit', pathMatch: 'full' },
      // Legacy placements (pre–Stage 5A "Platform" section)
      { path: 'platform/integrations', redirectTo: 'system/integrations', pathMatch: 'prefix' },
      { path: 'platform/licensing', redirectTo: 'system/licensing', pathMatch: 'full' },
      { path: 'platform/commercial', redirectTo: 'system/commercial', pathMatch: 'full' },
      { path: 'platform/compliance', redirectTo: 'system/compliance-defaults', pathMatch: 'full' },

      // ───────────────────────────── Account & workspace ──────────────────────────
      {
        path: 'account',
        loadComponent: () =>
          import('./pages/settings-account-page.component').then(
            (m) => m.SettingsAccountPageComponent,
          ),
      },
      {
        path: 'theme',
        loadComponent: () =>
          import('./pages/settings-theme-page.component').then((m) => m.SettingsThemePageComponent),
      },
      {
        path: 'about',
        loadComponent: () =>
          import('./pages/settings-about-page.component').then((m) => m.SettingsAboutPageComponent),
      },
    ],
  },
];

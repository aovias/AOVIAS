export interface SettingsNavItem {
  label: string;
  route: string;
  icon?: string;
  disabled?: boolean;
  comingSoon?: boolean;
  /** When true, active state matches only the exact route (not child paths). */
  exactMatch?: boolean;
}

export interface SettingsNavSection {
  id: string;
  title: string;
  items: SettingsNavItem[];
}

export const SETTINGS_BASE = '/app/settings';

/**
 * A tab of the Enterprise Administration Workspace.
 *
 * Stage 5A: the workspace exposes exactly these twelve tabs. Internal navigation is
 * tab-first — the tab strip selects a domain, the workspace aside lists that domain's
 * pages. No tab owns a nested sidebar of its own.
 */
export interface AdministrationTab {
  id: string;
  label: string;
  icon: string;
  /** Landing route when the tab is selected. */
  route: string;
  /** One-line description used on the workspace home cards. */
  description: string;
  /**
   * URL prefixes that mark this tab active. Includes legacy prefixes so the tab strip
   * highlights correctly while a redirect is resolving.
   */
  matchPrefixes: string[];
  items: SettingsNavItem[];
}

const B = SETTINGS_BASE;

/** Route bases, declared once so routes and navigation cannot drift apart. */
export const ADMIN_ROUTES = {
  organization: `${B}/organization`,
  programs: `${B}/programs`,
  templates: `${B}/templates`,
  frameworks: `${B}/frameworks`,
  risk: `${B}/risk`,
  workflow: `${B}/workflow`,
  notifications: `${B}/notifications`,
  approvals: `${B}/approvals`,
  teams: `${B}/teams`,
  security: `${B}/security`,
  ai: `${B}/ai`,
  system: `${B}/system`,
} as const;

const TABS: AdministrationTab[] = [
  {
    id: 'organization',
    label: 'Organization',
    icon: 'business',
    route: `${ADMIN_ROUTES.organization}/profile`,
    description: 'Organization profile, branding, structure, calendar, and regional settings.',
    matchPrefixes: [ADMIN_ROUTES.organization, `${B}/general`],
    items: [
      { label: 'Profile', route: `${ADMIN_ROUTES.organization}/profile`, icon: 'business' },
      { label: 'Branding', route: `${ADMIN_ROUTES.organization}/branding`, icon: 'palette' },
      {
        label: 'Facilities & Locations',
        route: `${ADMIN_ROUTES.organization}/facilities`,
        icon: 'apartment',
        comingSoon: true,
      },
      {
        label: 'Financial Year',
        route: `${ADMIN_ROUTES.organization}/financial-year`,
        icon: 'event_note',
        comingSoon: true,
      },
      {
        label: 'Business Calendar',
        route: `${ADMIN_ROUTES.organization}/calendar`,
        icon: 'calendar_month',
        comingSoon: true,
      },
      {
        label: 'Localization & Time Zone',
        route: `${ADMIN_ROUTES.organization}/localization`,
        icon: 'language',
        comingSoon: true,
      },
    ],
  },
  {
    id: 'programs',
    label: 'Programs',
    icon: 'event_repeat',
    route: ADMIN_ROUTES.programs,
    description: 'Annual, quarterly, risk-based, and department audit programs.',
    matchPrefixes: [ADMIN_ROUTES.programs],
    items: [
      { label: 'Overview', route: ADMIN_ROUTES.programs, icon: 'dashboard', exactMatch: true },
      {
        label: 'Program Schedule',
        route: `${ADMIN_ROUTES.programs}/schedule`,
        icon: 'calendar_month',
        comingSoon: true,
      },
      {
        label: 'Coverage & Progress',
        route: `${ADMIN_ROUTES.programs}/coverage`,
        icon: 'donut_large',
        comingSoon: true,
      },
    ],
  },
  {
    id: 'templates',
    label: 'Templates',
    icon: 'library_books',
    route: ADMIN_ROUTES.templates,
    description: 'Audit, checklist, evidence, finding, CAPA, and report template library.',
    matchPrefixes: [ADMIN_ROUTES.templates],
    items: [
      { label: 'Overview', route: ADMIN_ROUTES.templates, icon: 'dashboard', exactMatch: true },
      {
        label: 'Template Library',
        route: `${ADMIN_ROUTES.templates}/library`,
        icon: 'library_books',
        comingSoon: true,
      },
      {
        label: 'Versioning & Approval',
        route: `${ADMIN_ROUTES.templates}/versions`,
        icon: 'history_edu',
        comingSoon: true,
      },
    ],
  },
  {
    id: 'frameworks',
    label: 'Frameworks',
    icon: 'account_balance',
    route: ADMIN_ROUTES.frameworks,
    description: 'Framework catalog, versions, activation, and cross mapping.',
    matchPrefixes: [ADMIN_ROUTES.frameworks],
    items: [
      {
        label: 'Catalog',
        route: ADMIN_ROUTES.frameworks,
        icon: 'account_balance',
        exactMatch: true,
      },
      {
        label: 'Cross Mapping',
        route: `${ADMIN_ROUTES.frameworks}/cross-mapping`,
        icon: 'compare_arrows',
        comingSoon: true,
      },
    ],
  },
  {
    id: 'risk',
    label: 'Risk',
    icon: 'shield',
    route: ADMIN_ROUTES.risk,
    description: 'Risk matrix, formula, levels, severity matrix, and escalation thresholds.',
    matchPrefixes: [ADMIN_ROUTES.risk],
    items: [
      {
        label: 'Risk Matrix',
        route: ADMIN_ROUTES.risk,
        icon: 'grid_on',
        exactMatch: true,
        comingSoon: true,
      },
      {
        label: 'Severity Matrix',
        route: `${ADMIN_ROUTES.risk}/severity`,
        icon: 'priority_high',
        comingSoon: true,
      },
      {
        label: 'Escalation Thresholds',
        route: `${ADMIN_ROUTES.risk}/escalation`,
        icon: 'trending_up',
        comingSoon: true,
      },
    ],
  },
  {
    id: 'workflow',
    label: 'Workflow',
    icon: 'account_tree',
    route: ADMIN_ROUTES.workflow,
    description: 'Lifecycle, transitions, validation rules, and workflow versioning.',
    matchPrefixes: [ADMIN_ROUTES.workflow],
    items: [
      {
        label: 'Lifecycle',
        route: ADMIN_ROUTES.workflow,
        icon: 'account_tree',
        exactMatch: true,
        comingSoon: true,
      },
      {
        label: 'Transitions',
        route: `${ADMIN_ROUTES.workflow}/transitions`,
        icon: 'swap_horiz',
        comingSoon: true,
      },
      {
        label: 'Versions',
        route: `${ADMIN_ROUTES.workflow}/versions`,
        icon: 'history',
        comingSoon: true,
      },
    ],
  },
  {
    id: 'notifications',
    label: 'Notifications',
    icon: 'notifications',
    route: ADMIN_ROUTES.notifications,
    description: 'Templates, channels, delivery, preferences, and subscriptions.',
    matchPrefixes: [ADMIN_ROUTES.notifications, `${B}/platform/notifications`],
    items: [
      { label: 'Overview', route: ADMIN_ROUTES.notifications, icon: 'dashboard', exactMatch: true },
      { label: 'Templates', route: `${ADMIN_ROUTES.notifications}/templates`, icon: 'description' },
      { label: 'Channels', route: `${ADMIN_ROUTES.notifications}/channels`, icon: 'hub' },
      {
        label: 'Delivery Providers',
        route: `${ADMIN_ROUTES.notifications}/providers`,
        icon: 'settings_input_component',
      },
      { label: 'Queues', route: `${ADMIN_ROUTES.notifications}/queues`, icon: 'queue' },
      {
        label: 'Delivery History',
        route: `${ADMIN_ROUTES.notifications}/history`,
        icon: 'history',
      },
      { label: 'Preferences', route: `${ADMIN_ROUTES.notifications}/preferences`, icon: 'tune' },
      {
        label: 'Subscriptions',
        route: `${ADMIN_ROUTES.notifications}/subscriptions`,
        icon: 'subscriptions',
      },
    ],
  },
  {
    id: 'approvals',
    label: 'Approvals',
    icon: 'approval',
    route: `${ADMIN_ROUTES.approvals}/matrix`,
    description: 'Approval matrix, delegation, escalation, and digital signatures.',
    matchPrefixes: [ADMIN_ROUTES.approvals],
    items: [
      { label: 'Approval Matrix', route: `${ADMIN_ROUTES.approvals}/matrix`, icon: 'grid_view' },
      {
        label: 'Delegations',
        route: `${ADMIN_ROUTES.approvals}/delegations`,
        icon: 'switch_account',
      },
      {
        label: 'Approval Chains',
        route: `${ADMIN_ROUTES.approvals}/chains`,
        icon: 'linear_scale',
        comingSoon: true,
      },
      {
        label: 'Digital Signatures',
        route: `${ADMIN_ROUTES.approvals}/signatures`,
        icon: 'draw',
        comingSoon: true,
      },
    ],
  },
  {
    id: 'teams',
    label: 'Teams',
    icon: 'groups',
    route: `${ADMIN_ROUTES.teams}/dashboard`,
    description: 'Auditors, teams, directory, departments, designations, and membership.',
    matchPrefixes: [ADMIN_ROUTES.teams, `${B}/workforce`],
    items: [
      { label: 'Dashboard', route: `${ADMIN_ROUTES.teams}/dashboard`, icon: 'dashboard' },
      {
        label: 'Workforce Intelligence',
        route: `${ADMIN_ROUTES.teams}/intelligence`,
        icon: 'analytics',
      },
      {
        label: 'Enterprise Directory',
        route: `${ADMIN_ROUTES.teams}/directory`,
        icon: 'account_tree',
      },
      { label: 'Departments', route: `${ADMIN_ROUTES.teams}/departments`, icon: 'apartment' },
      { label: 'Designations', route: `${ADMIN_ROUTES.teams}/designations`, icon: 'badge' },
      {
        label: 'Professional Registry',
        route: `${ADMIN_ROUTES.teams}/professionals`,
        icon: 'verified_user',
      },
      { label: 'Membership', route: `${ADMIN_ROUTES.teams}/memberships`, icon: 'groups' },
      { label: 'Invitations', route: `${ADMIN_ROUTES.teams}/invitations`, icon: 'mail' },
      { label: 'Training', route: `${ADMIN_ROUTES.teams}/training`, icon: 'school' },
    ],
  },
  {
    id: 'security',
    label: 'Security',
    icon: 'security',
    route: `${ADMIN_ROUTES.security}/users`,
    description: 'Users, roles, permission matrix, policies, and security posture.',
    matchPrefixes: [ADMIN_ROUTES.security, `${B}/identity`],
    items: [
      { label: 'Users', route: `${ADMIN_ROUTES.security}/users`, icon: 'group' },
      { label: 'Roles', route: `${ADMIN_ROUTES.security}/roles`, icon: 'admin_panel_settings' },
      { label: 'Permission Catalog', route: `${ADMIN_ROUTES.security}/permissions`, icon: 'key' },
      { label: 'Access Policies', route: `${ADMIN_ROUTES.security}/policies`, icon: 'policy' },
      {
        label: 'Effective Permissions',
        route: `${ADMIN_ROUTES.security}/effective`,
        icon: 'fact_check',
      },
      {
        label: 'Authorization Simulator',
        route: `${ADMIN_ROUTES.security}/simulator`,
        icon: 'science',
      },
      {
        label: 'Password Management',
        route: `${ADMIN_ROUTES.security}/password`,
        icon: 'password',
      },
      { label: 'Security Posture', route: `${ADMIN_ROUTES.security}/posture`, icon: 'gpp_good' },
    ],
  },
  {
    id: 'ai',
    label: 'AI',
    icon: 'smart_toy',
    route: `${ADMIN_ROUTES.ai}/dashboard`,
    description: 'Identities, models, providers, prompts, policies, and usage analytics.',
    matchPrefixes: [ADMIN_ROUTES.ai, `${B}/identity/ai-identity`],
    items: [
      { label: 'Dashboard', route: `${ADMIN_ROUTES.ai}/dashboard`, icon: 'dashboard' },
      { label: 'AI Identity', route: `${ADMIN_ROUTES.ai}/identity`, icon: 'badge' },
      { label: 'Models', route: `${ADMIN_ROUTES.ai}/models`, icon: 'psychology' },
      { label: 'Providers', route: `${ADMIN_ROUTES.ai}/providers`, icon: 'cloud' },
      { label: 'Prompt Library', route: `${ADMIN_ROUTES.ai}/prompts`, icon: 'description' },
      { label: 'Policies', route: `${ADMIN_ROUTES.ai}/policies`, icon: 'policy' },
      { label: 'Permissions', route: `${ADMIN_ROUTES.ai}/permissions`, icon: 'key' },
      { label: 'Executions', route: `${ADMIN_ROUTES.ai}/executions`, icon: 'history' },
      { label: 'Usage & Analytics', route: `${ADMIN_ROUTES.ai}/analytics`, icon: 'insights' },
      { label: 'Approvals', route: `${ADMIN_ROUTES.ai}/approvals`, icon: 'task_alt' },
    ],
  },
  {
    id: 'system',
    label: 'System',
    icon: 'settings',
    route: `${ADMIN_ROUTES.system}/audit`,
    description: 'Audit trail, integrations, licensing, retention, and platform defaults.',
    matchPrefixes: [ADMIN_ROUTES.system, `${B}/platform`, `${B}/theme`, `${B}/about`],
    items: [
      { label: 'Audit & Activity', route: `${ADMIN_ROUTES.system}/audit`, icon: 'history' },
      { label: 'Integrations', route: `${ADMIN_ROUTES.system}/integrations`, icon: 'hub' },
      { label: 'Licensing', route: `${ADMIN_ROUTES.system}/licensing`, icon: 'vpn_key' },
      { label: 'Commercial Ops', route: `${ADMIN_ROUTES.system}/commercial`, icon: 'storefront' },
      {
        label: 'Compliance Defaults',
        route: `${ADMIN_ROUTES.system}/compliance-defaults`,
        icon: 'rule',
        comingSoon: true,
      },
      {
        label: 'Number Sequences',
        route: `${ADMIN_ROUTES.system}/number-sequences`,
        icon: 'tag',
        comingSoon: true,
      },
      {
        label: 'Retention & Storage',
        route: `${ADMIN_ROUTES.system}/retention`,
        icon: 'inventory_2',
        comingSoon: true,
      },
      {
        label: 'Feature Flags',
        route: `${ADMIN_ROUTES.system}/feature-flags`,
        icon: 'toggle_on',
        comingSoon: true,
      },
      { label: 'Theme', route: `${B}/theme`, icon: 'dark_mode' },
      { label: 'About AOVIAS Command', route: `${B}/about`, icon: 'info' },
    ],
  },
];

export function getAdministrationTabs(): AdministrationTab[] {
  return TABS;
}

/**
 * Resolve the tab owning a URL. Longest prefix wins so that `/settings/risk/severity`
 * cannot be claimed by a shorter, unrelated prefix.
 */
export function resolveAdministrationTab(url: string): AdministrationTab | undefined {
  const path = url.split(/[?#]/)[0].replace(/\/+$/, '');
  let best: AdministrationTab | undefined;
  let bestLength = 0;

  for (const tab of TABS) {
    for (const prefix of tab.matchPrefixes) {
      if ((path === prefix || path.startsWith(`${prefix}/`)) && prefix.length > bestLength) {
        best = tab;
        bestLength = prefix.length;
      }
    }
  }

  return best;
}

/**
 * Sections view of the tabs, retained for consumers that predate the tab strip
 * (breadcrumb resolution).
 */
export function getSettingsNavSections(): SettingsNavSection[] {
  return TABS.map((tab) => ({ id: tab.id, title: tab.label, items: tab.items }));
}

export type SettingsHomeCardStatus = 'configured' | 'coming-soon' | 'settings-count';

export interface SettingsHomeCard {
  title: string;
  description: string;
  route: string;
  icon: string;
  footer: string;
  status: SettingsHomeCardStatus;
}

/**
 * Home cards mirror the tabs one-for-one. `footer` and `status` report the tab's real
 * state — a tab whose pages are all `comingSoon` is labelled as such rather than as
 * configured.
 */
export function getSettingsHomeCards(): SettingsHomeCard[] {
  return TABS.map((tab) => {
    const available = tab.items.filter((item) => !item.comingSoon);
    const status: SettingsHomeCardStatus =
      available.length === 0 ? 'coming-soon' : 'settings-count';
    const footer =
      available.length === 0
        ? 'Coming Soon'
        : `${available.length} ${available.length === 1 ? 'area' : 'areas'}`;

    return {
      title: tab.label,
      description: tab.description,
      route: tab.route,
      icon: tab.icon,
      footer,
      status,
    };
  });
}

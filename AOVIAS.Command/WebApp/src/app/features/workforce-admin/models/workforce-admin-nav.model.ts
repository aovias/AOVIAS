/** Stage 5A: workforce administration is the Administration Workspace "Teams" tab. */
export const WORKFORCE_ADMIN_BASE = '/app/settings/teams';

export interface WorkforceAdminNavItem {
  label: string;
  route: string;
  segment: string;
  icon: string;
  enabled: boolean;
  comingSoon?: boolean;
}

export interface WorkforceAdminCapabilityCard {
  label: string;
  description: string;
  route?: string;
  icon: string;
  comingSoon?: boolean;
}

export function getWorkforceAdminNavItems(): WorkforceAdminNavItem[] {
  return [
    {
      label: 'Dashboard',
      route: `${WORKFORCE_ADMIN_BASE}/dashboard`,
      segment: 'dashboard',
      icon: 'dashboard',
      enabled: true,
    },
    {
      label: 'Workforce Intelligence',
      route: `${WORKFORCE_ADMIN_BASE}/intelligence`,
      segment: 'intelligence',
      icon: 'analytics',
      enabled: true,
    },
    {
      label: 'Enterprise Directory',
      route: `${WORKFORCE_ADMIN_BASE}/directory`,
      segment: 'directory',
      icon: 'account_tree',
      enabled: true,
    },
    {
      label: 'Departments',
      route: `${WORKFORCE_ADMIN_BASE}/departments`,
      segment: 'departments',
      icon: 'apartment',
      enabled: true,
    },
    {
      label: 'Designations',
      route: `${WORKFORCE_ADMIN_BASE}/designations`,
      segment: 'designations',
      icon: 'badge',
      enabled: true,
    },
    {
      label: 'Professional Registry',
      route: `${WORKFORCE_ADMIN_BASE}/professionals`,
      segment: 'professionals',
      icon: 'verified_user',
      enabled: true,
    },
    {
      label: 'Membership',
      route: `${WORKFORCE_ADMIN_BASE}/memberships`,
      segment: 'memberships',
      icon: 'groups',
      enabled: true,
    },
    {
      label: 'Invitations',
      route: `${WORKFORCE_ADMIN_BASE}/invitations`,
      segment: 'invitations',
      icon: 'mail',
      enabled: true,
    },
    {
      label: 'Training',
      route: `${WORKFORCE_ADMIN_BASE}/training`,
      segment: 'training',
      icon: 'menu_book',
      enabled: true,
    },
  ];
}

export function getWorkforceAdminCapabilityCards(): WorkforceAdminCapabilityCard[] {
  return getWorkforceAdminNavItems()
    .filter((i) => i.enabled && !i.comingSoon && i.segment !== 'dashboard')
    .map((i) => ({
      label: i.label,
      description: workforceCapabilityDescription(i.segment),
      route: i.route,
      icon: i.icon,
    }));
}

export function getWorkforceAdminFuturePlaceholders(): WorkforceAdminCapabilityCard[] {
  return [
    {
      label: 'Organization Chart',
      description: 'Interactive enterprise org chart and reporting lines.',
      icon: 'account_tree',
      comingSoon: true,
    },
    {
      label: 'Skills Matrix',
      description: 'Cross-functional skill coverage and gap analysis.',
      icon: 'grid_view',
      comingSoon: true,
    },
    {
      label: 'Competency Framework',
      description: 'Role competency models and proficiency levels.',
      icon: 'school',
      comingSoon: true,
    },
    {
      label: 'Training Matrix',
      description: 'Mandatory training coverage by role and department.',
      route: `${WORKFORCE_ADMIN_BASE}/training/matrix`,
      icon: 'menu_book',
    },
    {
      label: 'Succession Planning',
      description: 'Critical role successors and readiness tracking.',
      icon: 'trending_up',
      comingSoon: true,
    },
    {
      label: 'Credential Expiry Dashboard',
      description: 'Unified expiry and renewal risk overview.',
      icon: 'event_busy',
      comingSoon: true,
    },
    {
      label: 'Workforce Planning',
      description: 'Capacity planning, vacancies, and hiring forecasts.',
      icon: 'insights',
      comingSoon: true,
    },
  ];
}

export function resolveWorkforceAdminBreadcrumb(url: string): string {
  const path = url.split(/[?#]/)[0].replace(/\/+$/, '');
  if (path === WORKFORCE_ADMIN_BASE) {
    return 'Dashboard';
  }

  const items = getWorkforceAdminNavItems()
    .slice()
    .sort((a, b) => b.route.length - a.route.length);

  for (const item of items) {
    if (path === item.route) {
      return item.label;
    }
    if (item.segment && path.startsWith(`${WORKFORCE_ADMIN_BASE}/${item.segment}/`)) {
      return item.label;
    }
  }

  return 'Workforce';
}

function workforceCapabilityDescription(segment: string): string {
  switch (segment) {
    case 'intelligence':
      return 'Executive analytics for headcount, credentials, skills, and risk.';
    case 'directory':
      return 'Search people, departments, designations, and contact details.';
    case 'departments':
      return 'Organization department hierarchy and membership coverage.';
    case 'designations':
      return 'Designations used across membership and directory records.';
    case 'professionals':
      return 'Professional registrations, specialties, and verification status.';
    case 'memberships':
      return 'Organization membership, roles, departments, and status.';
    case 'invitations':
      return 'Email invitations, resend, cancel, and accept pipeline status.';
    case 'training':
      return 'Training programs, sessions, attendance, and competency matrix.';
    default:
      return 'Workforce administration.';
  }
}

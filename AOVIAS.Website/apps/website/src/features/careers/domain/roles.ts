import type { OpenRole } from '@/features/careers/domain/types';

const APPLY_MAIL = 'mailto:careers@aovias.com';

/**
 * Open roles registry — single source of truth for Careers listings.
 */
export const openRolesCatalog: readonly OpenRole[] = [
  {
    id: 'platform-engineer',
    slug: 'platform-engineer',
    title: 'Platform Engineer',
    department: 'Engineering',
    employmentType: 'full-time',
    location: 'Remote / Hybrid',
    status: 'open',
    remote: true,
    addressCountry: 'IN',
    publishedAt: '2026-06-01',
    summary:
      'Design and operate durable platform foundations that institutions can trust over long horizons.',
    description:
      'You will deepen reliability, observability, and delivery discipline across AOVIAS platform services. Success means clearer interfaces, measured operational quality, and systems that remain maintainable under institutional constraints.',
    applyHref: `${APPLY_MAIL}?subject=Application%20%E2%80%94%20Platform%20Engineer`,
  },
  {
    id: 'product-engineer-command',
    slug: 'product-engineer-command',
    title: 'Product Engineer — Command',
    department: 'Product Engineering',
    employmentType: 'full-time',
    location: 'Remote / Hybrid',
    status: 'open',
    remote: true,
    addressCountry: 'IN',
    publishedAt: '2026-06-15',
    summary:
      'Build Command capabilities with clarity, restraint, and respect for the people who depend on them.',
    description:
      'You will own end-to-end product engineering for Command workflows: thoughtful UX boundaries, rigorous testing, and collaborative design with domain experts. We value craftsmanship over novelty and evidence over hype.',
    applyHref: `${APPLY_MAIL}?subject=Application%20%E2%80%94%20Product%20Engineer%20Command`,
  },
  {
    id: 'responsible-ai-engineer',
    slug: 'responsible-ai-engineer',
    title: 'Responsible AI Engineer',
    department: 'Applied Research',
    employmentType: 'full-time',
    location: 'Remote / Hybrid',
    status: 'upcoming',
    remote: true,
    addressCountry: 'IN',
    publishedAt: '2026-07-01',
    summary:
      'Advance evaluation, governance, and practical AI systems that strengthen institutional judgment.',
    description:
      'You will help design evaluation practices, safety constraints, and applied AI workflows suited to regulated and mission-critical contexts. This role emphasizes responsibility and clarity over experimental spectacle.',
    applyHref: `${APPLY_MAIL}?subject=Application%20%E2%80%94%20Responsible%20AI%20Engineer`,
  },
] as const;

export function getOpenRoles(): readonly OpenRole[] {
  return openRolesCatalog;
}

/** Roles currently accepting applications. */
export function getActiveOpenRoles(): readonly OpenRole[] {
  return openRolesCatalog.filter((role) => role.status === 'open');
}

export function getRoleBySlug(slug: string): OpenRole | undefined {
  return openRolesCatalog.find((role) => role.slug === slug);
}

export const employmentTypeLabels: Record<OpenRole['employmentType'], string> = {
  'full-time': 'Full-time',
  'part-time': 'Part-time',
  contract: 'Contract',
  internship: 'Internship',
};

export const roleStatusLabels: Record<OpenRole['status'], string> = {
  open: 'Open',
  upcoming: 'Upcoming',
  filled: 'Filled',
  closed: 'Closed',
};

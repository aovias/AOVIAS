import type { MegaMenuColumn, NavGroup } from './types';

/**
 * Solutions / Industries navigation — Phase V6 primary industries first.
 */
export const solutionGroups: readonly NavGroup[] = [
  {
    id: 'industries',
    label: 'Industries',
    description: 'Sectors AOVIAS serves with dedicated institutional narratives',
    items: [
      {
        id: 'healthcare',
        label: 'Healthcare',
        href: '/solutions/healthcare',
        description: 'Clinical continuity and operational clarity',
        icon: 'shield',
      },
      {
        id: 'government',
        label: 'Government',
        href: '/solutions/government',
        description: 'Public accountability and service delivery',
        icon: 'building',
      },
      {
        id: 'manufacturing',
        label: 'Manufacturing',
        href: '/solutions/manufacturing',
        description: 'Production continuity and quality systems',
        icon: 'box',
      },
      {
        id: 'financial-services',
        label: 'Financial Services',
        href: '/solutions/financial-services',
        description: 'Trust, risk, and governed operations',
        icon: 'file',
      },
    ],
  },
  {
    id: 'more',
    label: 'More industries',
    description: 'Additional sectors and long-term direction',
    items: [
      {
        id: 'education',
        label: 'Education',
        href: '/solutions/education',
        description: 'Academic and administrative stewardship',
        icon: 'book',
      },
      {
        id: 'enterprise',
        label: 'Enterprise',
        href: '/solutions/enterprise',
        description: 'Governance and long-term value',
        icon: 'layers',
      },
      {
        id: 'energy',
        label: 'Energy',
        href: '/solutions/energy',
        description: 'Reliability for critical operations',
        icon: 'shield',
      },
      {
        id: 'transportation',
        label: 'Transportation',
        href: '/solutions/transportation',
        description: 'Coordination for complex networks',
        icon: 'arrowRight',
      },
    ],
  },
] as const;

export const solutionMegaColumns: readonly MegaMenuColumn[] = solutionGroups.map(
  (group) => ({
    id: group.id,
    title: group.label,
    items: group.items,
  }),
);

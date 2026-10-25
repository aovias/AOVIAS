import type { MegaMenuColumn, NavGroup } from './types';

/**
 * Product navigation — aligned with Product Detail Registry routes.
 */
export const productGroups: readonly NavGroup[] = [
  {
    id: 'platform',
    label: 'Platform',
    description: 'Core AOVIAS platform capabilities',
    items: [
      {
        id: 'command',
        label: 'AOVIAS Command',
        href: '/solutions/healthcare',
        description: 'Operational command and control',
        icon: 'box',
      },
      {
        id: 'studio',
        label: 'AOVIAS Studio',
        href: '/products/studio',
        description: 'Product capability and delivery workspace',
        icon: 'layers',
      },
      {
        id: 'platform',
        label: 'AOVIAS Platform',
        href: '/products/platform',
        description: 'Shared foundation for the product portfolio',
        icon: 'layers',
      },
    ],
  },
  {
    id: 'roadmap',
    label: 'Roadmap',
    description: 'Long-term platform vision',
    items: [
      {
        id: 'sentinel',
        label: 'AOVIAS Sentinel',
        href: '/products/sentinel',
        description: 'Assurance and institutional oversight',
        icon: 'shield',
      },
      {
        id: 'nexus',
        label: 'AOVIAS Nexus',
        href: '/products/nexus',
        description: 'Interoperability across institutional systems',
        icon: 'layers',
      },
      {
        id: 'insight',
        label: 'AOVIAS Insight',
        href: '/products/insight',
        description: 'Responsible intelligence and decision support',
        icon: 'search',
      },
      {
        id: 'flow',
        label: 'AOVIAS Flow',
        href: '/products/flow',
        description: 'Durable institutional workflows',
        icon: 'arrowRight',
      },
    ],
  },
] as const;

export const productMegaColumns: readonly MegaMenuColumn[] = productGroups.map((group) => ({
  id: group.id,
  title: group.label,
  items: group.items,
}));

import type { MegaMenuColumn, NavGroup, NavItem } from './types';

export const resourceItems: readonly NavItem[] = [
  {
    id: 'hub',
    label: 'Knowledge hub',
    href: '/resources',
    description: 'Browse the full knowledge platform',
    icon: 'layers',
  },
  {
    id: 'blog',
    label: 'Blog',
    href: '/resources/blog',
    description: 'Engineering articles and updates',
    icon: 'file',
  },
  {
    id: 'insights',
    label: 'Insights',
    href: '/resources/insights',
    description: 'Analysis and practical research',
    icon: 'search',
  },
  {
    id: 'documentation',
    label: 'Documentation',
    href: '/resources/docs',
    description: 'Product and implementation docs',
    icon: 'book',
  },
  {
    id: 'library',
    label: 'Resources',
    href: '/resources/library',
    description: 'Guides, checklists, and references',
    icon: 'layers',
  },
  {
    id: 'categories',
    label: 'Categories',
    href: '/resources/categories',
    description: 'Browse by topic',
    icon: 'file',
  },
  {
    id: 'search',
    label: 'Search',
    href: '/resources/search',
    description: 'Find knowledge across every section',
    icon: 'search',
  },
] as const;

export const resourceGroups: readonly NavGroup[] = [
  {
    id: 'knowledge',
    label: 'Knowledge',
    description: 'AOVIAS Knowledge Platform',
    items: resourceItems,
  },
] as const;

export const resourceMegaColumns: readonly MegaMenuColumn[] = resourceGroups.map(
  (group) => ({
    id: group.id,
    title: group.label,
    items: group.items,
  }),
);

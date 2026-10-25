import { companyMegaColumns } from './company';
import { productMegaColumns } from './products';
import { resourceMegaColumns } from './resources';
import { solutionMegaColumns } from './solutions';
import { technologyMegaColumns } from './technology';
import type { MainNavItem } from './types';

export const mainNavigation: readonly MainNavItem[] = [
  {
    id: 'products',
    label: 'Products',
    href: '/products',
    megaMenu: {
      id: 'products-mega',
      columns: productMegaColumns,
      cta: {
        id: 'products-pricing',
        label: 'View pricing',
        href: '/pricing',
        description: 'Studio and Command commercial options',
      },
    },
  },
  {
    id: 'solutions',
    label: 'Solutions',
    href: '/solutions',
    megaMenu: {
      id: 'solutions-mega',
      columns: solutionMegaColumns,
      cta: {
        id: 'solutions-overview',
        label: 'View all industries',
        href: '/solutions',
        description: 'Explore AOVIAS industry experiences',
      },
    },
  },
  {
    id: 'technology',
    label: 'Technology',
    href: '/technology',
    megaMenu: {
      id: 'technology-mega',
      columns: technologyMegaColumns,
      cta: {
        id: 'technology-platform',
        label: 'Explore AOVIAS Platform',
        href: '/technology/platform',
        description: 'One enterprise platform powering every AOVIAS product',
      },
    },
  },
  {
    id: 'resources',
    label: 'Resources',
    href: '/resources',
    megaMenu: {
      id: 'resources-mega',
      columns: resourceMegaColumns,
      cta: {
        id: 'docs-home',
        label: 'Open documentation',
        href: '/resources/docs',
        description: 'Product documentation and implementation guides',
      },
    },
  },
  {
    id: 'company',
    label: 'Company',
    href: '/company',
    megaMenu: {
      id: 'company-mega',
      columns: companyMegaColumns,
    },
  },
] as const;

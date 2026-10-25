import type { MegaMenuColumn, NavItem } from './types';

export const companyItems: readonly NavItem[] = [
  {
    id: 'about',
    label: 'About',
    href: '/company',
    description: 'Mission, principles, and leadership',
    icon: 'building',
  },
  {
    id: 'careers',
    label: 'Careers',
    href: '/company/careers',
    description: 'Join the AOVIAS institution',
    icon: 'users',
  },
  {
    id: 'contact',
    label: 'Contact',
    href: '/company/contact',
    description: 'Talk with our team',
    icon: 'external',
  },
] as const;

export const companyMegaColumns: readonly MegaMenuColumn[] = [
  {
    id: 'company',
    title: 'Company',
    items: companyItems,
  },
] as const;

import { companyItems } from './company';
import { productGroups } from './products';
import { resourceItems } from './resources';
import { solutionGroups } from './solutions';
import { technologyFooterItems } from './technology';
import type { FooterColumn, NavItem, SocialLink } from './types';

const productFooterItems: readonly NavItem[] = productGroups.flatMap((group) => group.items);

const industryFooterItems: readonly NavItem[] =
  solutionGroups.find((group) => group.id === 'industries')?.items ?? [];

export const footerColumns: readonly FooterColumn[] = [
  { id: 'products', title: 'Products', items: productFooterItems },
  { id: 'solutions', title: 'Solutions', items: industryFooterItems },
  { id: 'technology', title: 'Technology', items: technologyFooterItems },
  {
    id: 'resources',
    title: 'Resources',
    items: resourceItems
      .filter((item) =>
        ['blog', 'insights', 'documentation', 'library'].includes(item.id),
      )
      .map((item) =>
        item.id === 'library' ? { ...item, label: 'Library' } : item,
      ),
  },
  { id: 'company', title: 'Company', items: companyItems },
  {
    id: 'legal',
    title: 'Legal',
    items: [
      { id: 'privacy', label: 'Privacy', href: '/legal/privacy' },
      { id: 'terms', label: 'Terms', href: '/legal/terms' },
      { id: 'cookies', label: 'Cookie Policy', href: '/legal/cookies' },
      { id: 'security', label: 'Security', href: '/legal/security' },
      { id: 'contact', label: 'Contact', href: '/company/contact' },
    ],
  },
] as const;

export const socialLinks: readonly SocialLink[] = [
  {
    id: 'linkedin',
    label: 'LinkedIn',
    href: 'https://www.linkedin.com/company/aovias',
    icon: 'external',
  },
  { id: 'x', label: 'X', href: 'https://x.com/aovias', icon: 'external' },
] as const;

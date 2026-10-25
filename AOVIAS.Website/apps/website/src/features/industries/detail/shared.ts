import type { ProductNavItem } from '@/components/product';

export const standardIndustryNav: readonly ProductNavItem[] = [
  { id: 'overview', label: 'Overview', href: '#overview' },
  { id: 'challenges', label: 'Challenges', href: '#challenges' },
  { id: 'solutions', label: 'Solutions', href: '#solutions' },
  { id: 'products', label: 'Products', href: '#products' },
  { id: 'benefits', label: 'Benefits', href: '#benefits' },
  { id: 'trust', label: 'Trust', href: '#trust' },
  { id: 'resources', label: 'Resources', href: '#resources' },
  { id: 'cta', label: 'Next steps', href: '#cta' },
] as const;

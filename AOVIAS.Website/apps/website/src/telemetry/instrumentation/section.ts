/**
 * Map pathname → logical site section for page view enrichment.
 */

export type SiteSection =
  | 'home'
  | 'company'
  | 'products'
  | 'pricing'
  | 'technology'
  | 'solutions'
  | 'resources'
  | 'legal'
  | 'auth'
  | 'checkout'
  | 'other';

export function resolveSiteSection(pathname: string): SiteSection {
  if (pathname === '/') return 'home';
  if (pathname.startsWith('/company')) return 'company';
  if (pathname.startsWith('/products')) return 'products';
  if (pathname.startsWith('/pricing')) return 'pricing';
  if (pathname.startsWith('/technology')) return 'technology';
  if (pathname.startsWith('/solutions')) return 'solutions';
  if (pathname.startsWith('/resources')) return 'resources';
  if (pathname.startsWith('/legal')) return 'legal';
  if (pathname.startsWith('/auth')) return 'auth';
  if (pathname.startsWith('/checkout')) return 'checkout';
  return 'other';
}

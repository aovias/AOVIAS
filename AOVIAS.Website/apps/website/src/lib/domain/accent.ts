/**
 * Resolve domain accent data attributes from a pathname.
 * Consumed by layout shells — CSS maps these to --domain-accent.
 */

export type DomainAccentAttrs = {
  'data-product'?: string;
  'data-technology'?: string;
  'data-industry'?: string;
};

const PRODUCT_SLUGS = new Set(['platform', 'command', 'studio']);
const INDUSTRY_SLUGS = new Set([
  'healthcare',
  'government',
  'education',
  'enterprise',
  'manufacturing',
  'financial-services',
]);

const TECH_SEGMENTS: Record<string, string> = {
  platform: 'platform',
  engineering: 'engineering',
  ai: 'ai',
  trust: 'trust',
  security: 'security',
  identity: 'identity',
  architecture: 'architecture',
  integrations: 'platform',
  developer: 'platform',
  apis: 'platform',
  why: 'platform',
  roadmap: 'platform',
};

/**
 * Map URL path → data-* attributes for subtle domain accents.
 */
export function resolveDomainAccentAttrs(pathname: string): DomainAccentAttrs {
  const segments = pathname.split('/').filter(Boolean);
  /* Homepage — platform indigo so hero eyebrows/diagrams are chromatic */
  if (segments.length === 0) return { 'data-technology': 'platform' };

  if (segments[0] === 'products') {
    if (segments.length === 1) return { 'data-product': 'platform' };
    const slug = segments[1];
    if (slug && PRODUCT_SLUGS.has(slug)) {
      return { 'data-product': slug };
    }
    if (slug && slug !== 'preview') {
      return { 'data-product': 'future' };
    }
    return { 'data-product': 'platform' };
  }

  if (segments[0] === 'pricing' || segments[0] === 'checkout') {
    return { 'data-product': 'platform' };
  }

  if (segments[0] === 'technology') {
    if (segments.length === 1) return { 'data-technology': 'platform' };
    const hub = segments[1];
    if (hub && TECH_SEGMENTS[hub]) {
      return { 'data-technology': TECH_SEGMENTS[hub] };
    }
    return { 'data-technology': 'platform' };
  }

  if (segments[0] === 'solutions') {
    if (segments.length === 1) return { 'data-industry': 'enterprise' };
    const slug = segments[1];
    if (slug && INDUSTRY_SLUGS.has(slug)) {
      return { 'data-industry': slug };
    }
  }

  return {};
}

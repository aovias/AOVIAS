/**
 * Page metadata type taxonomy for the SEO builder.
 * Maps to Open Graph `type` and default robots behavior.
 */
export type PageMetadataType =
  | 'website'
  | 'article'
  | 'product'
  | 'profile'
  | 'documentation'
  | 'legal'
  | 'marketing'
  | 'company';

export type OpenGraphType = 'website' | 'article' | 'profile';

const ogTypeByPage: Record<PageMetadataType, OpenGraphType> = {
  website: 'website',
  article: 'article',
  product: 'website',
  profile: 'profile',
  documentation: 'website',
  legal: 'website',
  marketing: 'website',
  company: 'website',
};

export function resolveOpenGraphType(pageType: PageMetadataType): OpenGraphType {
  return ogTypeByPage[pageType];
}

/** Default robots for page types (overridable per page). */
export const defaultRobotsByType: Record<
  PageMetadataType,
  { index: boolean; follow: boolean }
> = {
  website: { index: true, follow: true },
  article: { index: true, follow: true },
  product: { index: true, follow: true },
  profile: { index: true, follow: true },
  documentation: { index: true, follow: true },
  legal: { index: true, follow: true },
  marketing: { index: true, follow: true },
  company: { index: true, follow: true },
};

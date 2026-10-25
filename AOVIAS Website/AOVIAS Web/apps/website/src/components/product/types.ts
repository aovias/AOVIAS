import type { MarketingCta } from '@/components/marketing/types';

/** Re-export CTA shape for product consumers. */
export type ProductCta = MarketingCta;

export type ProductAlign = 'start' | 'center';

export type FeatureHighlight = {
  id: string;
  label: string;
};

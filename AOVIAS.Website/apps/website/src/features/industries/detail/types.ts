import type { ContentItem } from '@/components/content';
import type { MarketingCta } from '@/components/marketing/types';
import type { ProductNavItem } from '@/components/product';
import type {
  ComplianceFramework,
  SecurityControl,
  TrustSignal,
} from '@/components/trust';
import type {
  IndustryBenefit,
  IndustryChallenge,
  IndustryStatus,
} from '@/features/industries/domain';

export type IndustryDetailSeo = {
  title: string;
  description: string;
  keywords?: readonly string[];
};

export type IndustrySolutionPoint = {
  id: string;
  heading: string;
  description: string;
};

/**
 * Industry Detail Framework payload — standard template for every industry page.
 */
export type IndustryDetailContent = {
  slug: string;
  name: string;
  status: IndustryStatus;
  seo: IndustryDetailSeo;
  navigation: readonly ProductNavItem[];

  hero: {
    eyebrow?: string;
    headline: string;
    /** Short challenge framing under the headline. */
    challengesSummary: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
    illustrationLabel: string;
  };

  overview: {
    heading: string;
    description: string;
    opportunities: string;
  };

  challenges: {
    heading: string;
    description?: string;
    items: readonly IndustryChallenge[];
  };

  solutions: {
    heading: string;
    description: string;
    items: readonly IndustrySolutionPoint[];
  };

  /** Product catalog ids — resolved at render time. */
  products: {
    heading: string;
    description: string;
    productIds: readonly string[];
  };

  benefits: {
    heading: string;
    description?: string;
    items: readonly IndustryBenefit[];
  };

  trust: {
    heading: string;
    description?: string;
    signals?: readonly TrustSignal[];
    highlights?: readonly string[];
    controls?: readonly SecurityControl[];
    frameworks?: readonly ComplianceFramework[];
    cta?: MarketingCta;
  };

  resources: {
    heading: string;
    description?: string;
    items: readonly ContentItem[];
  };

  primaryCtaBand: {
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
  };
};

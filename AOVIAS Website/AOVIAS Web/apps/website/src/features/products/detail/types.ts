import type { EnterpriseAtmosphereVariant } from '@/components/brand';
import type { ContentItem } from '@/components/content';
import type { MarketingCta } from '@/components/marketing/types';
import type {
  CapabilityItem,
  FeatureHighlight,
  IntegrationItem,
  MediaSlot,
  ProductCta,
  ProductNavItem,
  ProductOverviewMetric,
  SpecCategory,
} from '@/components/product';
import type {
  ComplianceFramework,
  SecurityControl,
  TrustSignal,
} from '@/components/trust';
import type { ProductStatus } from '@/features/products/domain';
import type { AvlfAtmosphereId } from '@/registries/avlf';

/**
 * Product Detail Framework payload — standard template for every AOVIAS product page.
 * Future products supply configuration; they do not invent new page structures.
 */
export type ProductFeatureShowcaseItem = {
  id: string;
  title: string;
  description: string;
  media?: MediaSlot;
  align?: 'start' | 'end';
};

export type ProductDetailSeo = {
  title: string;
  description: string;
  keywords?: readonly string[];
};

export type ProductDetailContent = {
  /** Registry identity */
  slug: string;
  status: ProductStatus;

  /** Hero */
  name: string;
  tagline: string;
  description: string;
  badge?: string;
  /** Product personality atmosphere (Command / Studio / Platform). */
  atmosphere?: AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  highlights?: readonly FeatureHighlight[];
  heroMedia?: MediaSlot;
  /** Optional secondary hero media (e.g. compact workflow beside workspace). */
  secondaryHeroMedia?: MediaSlot;
  primaryCta?: ProductCta;
  secondaryCta?: ProductCta;

  /** Page SEO (consumed by the route) */
  seo: ProductDetailSeo;

  /** Sticky in-page navigation */
  navigation: readonly ProductNavItem[];

  overview: {
    heading: string;
    description: string;
    /** Who it serves / business value narrative (optional second paragraph). */
    audience?: string;
    media?: MediaSlot;
    metrics?: readonly ProductOverviewMetric[];
  };

  /** Key capabilities via Feature Showcase (+ optional CapabilityGrid summary). */
  capabilities: {
    heading: string;
    description?: string;
    showcases: readonly ProductFeatureShowcaseItem[];
    summaryItems?: readonly CapabilityItem[];
  };

  architecture: {
    title: string;
    description?: string;
    diagram: MediaSlot;
    caption?: string;
  };

  integrations: {
    heading: string;
    description?: string;
    items: readonly IntegrationItem[];
  };

  specifications: {
    heading: string;
    description?: string;
    categories: readonly SpecCategory[];
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

  pricingEntry: {
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
  };

  resources: {
    heading: string;
    description?: string;
    items: readonly ContentItem[];
  };

  /** Closing primary CTA band */
  primaryCtaBand: {
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
  };
};

/** @deprecated Use ProductDetailContent — retained for transitional imports. */
export type ProductLandingContent = ProductDetailContent;

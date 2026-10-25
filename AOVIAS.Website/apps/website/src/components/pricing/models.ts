import type { MarketingCta } from '@/components/marketing/types';
import type { FaqItem } from '@/components/trust/models';

/**
 * Pricing Domain Layer — structured commerce models for presentation.
 * Update data, not UI, when products, plans, or licensing change.
 */

export type CurrencyCode = string;

export type Currency = {
  code: CurrencyCode;
  symbol: string;
  /** BCP 47 locale for number formatting. */
  locale?: string;
};

/** Billing period definition — monthly, annual, or future cycles. */
export type BillingCycle = {
  id: string;
  label: string;
  /** Length in months (1 = monthly, 12 = annual). */
  months: number;
  /** Optional savings callout (e.g. "Save 20%"). */
  discountLabel?: string;
};

export type FeatureAvailability =
  | 'included'
  | 'excluded'
  | 'limited'
  | 'addon'
  | 'contact';

export type Feature = {
  id: string;
  name: string;
  description?: string;
  categoryId?: string;
};

export type FeatureCategory = {
  id: string;
  name: string;
  description?: string;
  /** Display order hint. */
  order?: number;
};

/** Alias used in some commerce contexts. */
export type FeatureGroup = FeatureCategory;

export type PlanFeature = {
  featureId: string;
  availability: FeatureAvailability;
  /** Short note for limited / addon cells. */
  note?: string;
};

export type PlanPrice = {
  cycleId: string;
  /** Null means custom / contact pricing. */
  amount: number | null;
  currencyCode: CurrencyCode;
  /** Override formatted display (e.g. "Custom"). */
  displayOverride?: string;
};

export type PricingPlanVariant = 'standard' | 'enterprise';

export type PricingPlan = {
  id: string;
  name: string;
  description?: string;
  badge?: string;
  featured?: boolean;
  variant?: PricingPlanVariant;
  prices: readonly PlanPrice[];
  features: readonly PlanFeature[];
  /** Convenience bullets when a full matrix is not needed. */
  highlights?: readonly string[];
  cta?: MarketingCta;
  secondaryCta?: MarketingCta;
};

export type Product = {
  id: string;
  name: string;
  description?: string;
  planIds: readonly string[];
};

export type PlanComparison = {
  id: string;
  productId?: string;
  planIds: readonly string[];
  featureIds: readonly string[];
  categoryIds?: readonly string[];
};

export type PricingFaqItem = FaqItem;

export type EnterpriseOffering = {
  id: string;
  title: string;
  description?: string;
  primaryCta: MarketingCta;
  /** e.g. Request demo */
  secondaryCta?: MarketingCta;
  /** e.g. Book consultation */
  tertiaryCta?: MarketingCta;
};

export type Addon = {
  id: string;
  name: string;
  description?: string;
  prices?: readonly PlanPrice[];
};

export type License = {
  id: string;
  name: string;
  description?: string;
  /** e.g. seat, capacity, site */
  model?: string;
};

export type PricingBannerTone = 'info' | 'success' | 'warning' | 'neutral';

export type PricingBannerMessage = {
  id: string;
  title?: string;
  message: string;
  tone?: PricingBannerTone;
  href?: string;
  linkLabel?: string;
};

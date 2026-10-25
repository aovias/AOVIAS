/**
 * Shared Industry domain model — configuration drives every industry page.
 */

export type IndustryStatus = 'available' | 'planned';

export type IndustryDefinition = {
  id: string;
  name: string;
  slug: string;
  summary: string;
  description: string;
  status: IndustryStatus;
  /** Product catalog ids recommended for this industry. */
  recommendedProductIds: readonly string[];
  order: number;
  href: string;
};

export type IndustryChallenge = {
  id: string;
  title: string;
  description: string;
};

export type IndustryBenefit = {
  id: string;
  heading: string;
  description: string;
};

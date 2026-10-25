import type { ReactNode } from 'react';

import type { EnterpriseAtmosphereVariant } from '@/components/brand';
import type { MarketingCta } from '@/components/marketing/types';
import type { AvlfAtmosphereId } from '@/registries/avlf';

export type CommerceJourneyStep = string;

export type CommerceComparisonColumn = {
  id: string;
  name: string;
};

export type CommerceComparisonRow = {
  id: string;
  label: string;
  values: Record<string, string>;
};

export type CommercePlanSummary = {
  id: string;
  name: string;
  audience: string;
  deployment: string;
  support: string;
  governance: string;
  engagement: string;
};

/**
 * Commerce editorial content — enterprise procurement rhythm (Phase V9).
 */
export type CommerceEditorialContent = {
  id: string;
  atmosphere: AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  hero: {
    eyebrow: string;
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
    illustration: ReactNode;
  };
  philosophy: {
    eyebrow?: string;
    title: string;
    description: string;
    principles: readonly { id: string; title: string; description: string }[];
  };
  plans: {
    eyebrow?: string;
    title: string;
    description: string;
    summaries: readonly CommercePlanSummary[];
  };
  comparison: {
    eyebrow?: string;
    title: string;
    description: string;
    columns: readonly CommerceComparisonColumn[];
    rows: readonly CommerceComparisonRow[];
  };
  illustrations?: {
    eyebrow?: string;
    title: string;
    description: string;
    items: readonly {
      id: string;
      title: string;
      description: string;
      illustration: ReactNode;
    }[];
  };
  journey: {
    eyebrow?: string;
    title: string;
    description: string;
    steps: readonly CommerceJourneyStep[];
  };
  closing: {
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
  };
};

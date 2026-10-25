import type { ReactNode } from 'react';

import type { EnterpriseAtmosphereVariant } from '@/components/brand';
import type { MarketingCta } from '@/components/marketing/types';
import type { AvlfAtmosphereId } from '@/registries/avlf';

/**
 * Technology Editorial Framework — shared rhythm for Platform, AI,
 * Engineering, Security, and Integrations hubs (Phase V5).
 */
export type TechnologyEditorialSection = {
  eyebrow?: string;
  title: string;
  description: string;
};

export type TechnologyEditorialCard = {
  id: string;
  title: string;
  description: string;
};

export type TechnologyEditorialContent = {
  /** Registry / analytics identity */
  id: string;
  /** Hero + page atmosphere — unique per pillar */
  atmosphere: AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  hero: {
    eyebrow: string;
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
    illustration: ReactNode;
  };
  /** Core philosophy — one message */
  principle: TechnologyEditorialSection;
  /** ONE architecture diagram */
  architecture: TechnologyEditorialSection & {
    caption?: string;
    diagram: ReactNode;
  };
  /** System story as ordered steps */
  story: TechnologyEditorialSection & {
    steps: readonly string[];
  };
  /** Engineering / design practice cards */
  practices: TechnologyEditorialSection & {
    items: readonly TechnologyEditorialCard[];
  };
  /** ONE workflow illustration */
  workflow: TechnologyEditorialSection & {
    steps: readonly string[];
  };
  /** 4–6 technical highlight cards */
  highlights: TechnologyEditorialSection & {
    items: readonly TechnologyEditorialCard[];
  };
  closing: {
    title: string;
    description: string;
    primaryCta: MarketingCta;
  };
};

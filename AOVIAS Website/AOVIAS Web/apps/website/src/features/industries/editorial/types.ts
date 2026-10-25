import type { LucideIcon } from 'lucide-react';
import type { ReactNode } from 'react';

import type { EnterpriseAtmosphereVariant } from '@/components/brand';
import type { DomainSceneVariant } from '@/components/brand/domain-scene';
import type { MarketingCta, MarketingHighlight } from '@/components/marketing/types';
import type { FaqItem } from '@/components/trust/models';
import type { AvlfAtmosphereId } from '@/registries/avlf';

export type IndustryHeroCta = MarketingCta & {
  icon?: LucideIcon;
};

export type IndustryHeroActionLink = MarketingCta & {
  id?: string;
  icon?: LucideIcon;
};

export type IndustryEditorialCard = {
  id: string;
  title: string;
  description: string;
};

export type IndustryPortfolioRole = {
  id: 'command' | 'studio' | 'platform';
  name: string;
  role: string;
  href: string;
};

export type IndustryNarrative = {
  id: string;
  title: string;
  description: string;
};

export type IndustryDemoVideo = {
  id: string;
  title: string;
  description: string;
  youtubeId: string;
  duration?: string;
  theme?: 'light' | 'dark';
  badge?: string;
};

export type IndustryDemoSection = {
  eyebrow?: string;
  title: string;
  description: string;
  runtimeLabel?: string;
  /** Optional real video URL; placeholder UI when omitted. */
  videoSrc?: string;
  posterSrc?: string;
  /** Prefer for multi-demo YouTube showcases (healthcare). */
  videos?: readonly IndustryDemoVideo[];
};

export type IndustryProductShot = {
  id: string;
  /** Audience / workspace category (e.g. Leadership, Quality). */
  category?: string;
  title: string;
  description: string;
  /** Three focused capability bullets. */
  capabilities?: readonly string[];
  /** Public path to screenshot — prefer real product captures when available. */
  imageSrc: string;
  imageAlt: string;
  /** Presentation frame; mobile uses a device shell. */
  frame?: 'desktop' | 'mobile';
};

export type IndustryProductExperience = {
  eyebrow?: string;
  title: string;
  description: string;
  items: readonly IndustryProductShot[];
};

export type IndustryOutcomes = {
  eyebrow?: string;
  title: string;
  description: string;
  items: readonly IndustryEditorialCard[];
};

export type IndustryComparison = {
  eyebrow?: string;
  title: string;
  description: string;
  traditionalLabel: string;
  modernLabel: string;
  traditional: readonly string[];
  modern: readonly string[];
};

export type IndustryIntegrationNode = {
  id: string;
  label: string;
};

export type IndustryIntegrations = {
  eyebrow?: string;
  title: string;
  description: string;
  hubLabel: string;
  nodes: readonly IndustryIntegrationNode[];
};

export type IndustryTrustPillars = {
  eyebrow?: string;
  title: string;
  description: string;
  items: readonly IndustryEditorialCard[];
};

export type IndustryBrochure = {
  eyebrow?: string;
  title: string;
  description: string;
  coverSrc: string;
  coverAlt: string;
  downloadCta: MarketingCta;
};

export type IndustryMidCta = {
  id: string;
  title: string;
  description?: string;
  primaryCta: MarketingCta;
  secondaryCta?: MarketingCta;
};

export type IndustryFaqSection = {
  eyebrow?: string;
  title: string;
  description?: string;
  items: readonly FaqItem[];
};

/**
 * Industry Editorial Framework — V6 institutional narratives.
 * Optional landing blocks are additive (healthcare conversion sections).
 */
export type IndustryEditorialContent = {
  slug: string;
  name: string;
  atmosphere: AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  domainVariant: DomainSceneVariant;
  /** Optional environment illustration (distinct from hero). */
  environmentVariant?: DomainSceneVariant;
  seo: {
    title: string;
    description: string;
    keywords?: readonly string[];
  };
  hero: {
    eyebrow: string;
    title: string;
    description: string;
    primaryCta: IndustryHeroCta;
    secondaryCta?: IndustryHeroCta;
    tertiaryCta?: IndustryHeroCta;
    /** Capability grid or trust strip under hero copy. */
    highlights?: readonly MarketingHighlight[];
    /** Text links under CTAs (consultation / walkthrough). */
    actionLinks?: readonly IndustryHeroActionLink[];
    illustrationCaption: string;
  };
  reality: {
    eyebrow?: string;
    title: string;
    description: string;
    pillars: readonly string[];
  };
  challenges: {
    eyebrow?: string;
    title: string;
    description: string;
    items: readonly IndustryEditorialCard[];
  };
  workflow: {
    eyebrow?: string;
    title: string;
    description: string;
    steps: readonly string[];
  };
  portfolio: {
    eyebrow?: string;
    title: string;
    description: string;
    items: readonly IndustryPortfolioRole[];
  };
  journey: {
    eyebrow?: string;
    title: string;
    description: string;
    steps: readonly string[];
    /**
     * `operating-cycle` — continuous governance model (healthcare).
     * Default path keeps the legacy stacked story flow for other industries.
     */
    model?: 'path' | 'operating-cycle';
    /** Rich step copy when model is operating-cycle. */
    cycleSteps?: readonly {
      id: string;
      label: string;
      summary: string;
    }[];
    /** Supporting paragraph under the section intro. */
    lead?: string;
  };
  narratives: {
    eyebrow?: string;
    title: string;
    description: string;
    items: readonly IndustryNarrative[];
    /**
     * `transformation` — executive before/after briefing (healthcare).
     * Default keeps scenario cards + DomainScene for other industries.
     */
    model?: 'scenarios' | 'transformation';
    /** Flow steps for the orchestration diagram. */
    flowSteps?: readonly { id: string; label: string }[];
    /** Horizontal outcome strip labels. */
    outcomeStrip?: readonly string[];
  };
  closing: {
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
    tertiaryCta?: MarketingCta;
  };
  /** Optional custom hero illustration override */
  heroIllustration?: ReactNode;

  /** Conversion landing extensions (optional — healthcare). */
  demo?: IndustryDemoSection;
  productExperience?: IndustryProductExperience;
  outcomes?: IndustryOutcomes;
  comparison?: IndustryComparison;
  integrations?: IndustryIntegrations;
  trustPillars?: IndustryTrustPillars;
  brochure?: IndustryBrochure;
  faq?: IndustryFaqSection;
  /** Mid-page CTA strips keyed by placement id. */
  midCtas?: readonly IndustryMidCta[];
};

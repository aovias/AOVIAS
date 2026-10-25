import type { ContentKind } from '@/components/content';
import type { EnterpriseAtmosphereVariant } from '@/components/brand';
import type { KnowledgeCoverTone } from '@/components/brand/knowledge-cover';
import type { MarketingCta } from '@/components/marketing/types';
import type { AvlfAtmosphereId } from '@/registries/avlf';
import type { ReactNode } from 'react';

export type KnowledgeLibraryCategory = {
  id: string;
  name: string;
  href: string;
  description?: string;
};

export type KnowledgeLearningPath = {
  title: string;
  description: string;
  steps: readonly string[];
};

/**
 * Knowledge section editorial payload — Blog, Insights, Docs, Library hubs.
 */
export type KnowledgeEditorialContent = {
  id: string;
  atmosphere: AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  cover: {
    label: string;
    tone?: KnowledgeCoverTone;
    motif?: 'ai' | 'security' | 'operations' | 'architecture' | 'default';
  };
  /** Content kinds shown in this section’s editorial grid */
  kinds: readonly ContentKind[];
  hero: {
    eyebrow: string;
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
  };
  orient: {
    eyebrow?: string;
    title: string;
    description: string;
  };
  libraryNav: {
    eyebrow?: string;
    title: string;
    description: string;
    categories: readonly KnowledgeLibraryCategory[];
  };
  featured: {
    eyebrow?: string;
    title: string;
    description: string;
  };
  grid: {
    eyebrow?: string;
    title: string;
    description: string;
  };
  learningPath: KnowledgeLearningPath;
  closing: {
    title: string;
    description: string;
    primaryCta: MarketingCta;
    secondaryCta?: MarketingCta;
  };
  /** Optional illustration override */
  illustration?: ReactNode;
};

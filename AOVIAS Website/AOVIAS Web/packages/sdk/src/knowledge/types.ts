import type { Page, PageRequest } from '../core/pagination';

/**
 * Knowledge / content domain — portable, CMS-agnostic.
 * Kind values align with website ContentKind for R1.5 static catalog.
 */

export type KnowledgeKind =
  | 'article'
  | 'insight'
  | 'documentation'
  | 'case-study'
  | 'whitepaper'
  | 'release-note'
  | 'academy-lesson'
  | 'resource';

export type KnowledgeCategory = {
  slug: string;
  name: string;
  description?: string;
};

export type KnowledgeArticle = {
  id: string;
  slug: string;
  title: string;
  description?: string;
  kind: KnowledgeKind;
  href: string;
  category?: KnowledgeCategory;
  tags?: readonly string[];
  publishedAt?: string;
  featured?: boolean;
};

export type KnowledgeListRequest = PageRequest & {
  kind?: KnowledgeKind;
  categorySlug?: string;
  featuredOnly?: boolean;
};

export type KnowledgeListResult = Page<KnowledgeArticle>;

export type KnowledgeSearchHit = {
  article: KnowledgeArticle;
  score: number;
  highlights: readonly string[];
};

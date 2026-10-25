import type { WithSdkInfo } from '../core/client';
import type { SdkResult } from '../core/result';
import type {
  KnowledgeArticle,
  KnowledgeCategory,
  KnowledgeListRequest,
  KnowledgeListResult,
  KnowledgeSearchHit,
} from './types';

/**
 * Knowledge SDK — articles, categories, related content, search over static sources.
 */
export type KnowledgeClient = WithSdkInfo & {
  listArticles: (
    request?: KnowledgeListRequest,
  ) => Promise<SdkResult<KnowledgeListResult>>;

  getArticle: (slug: string) => Promise<SdkResult<KnowledgeArticle | null>>;

  listCategories: () => Promise<SdkResult<readonly KnowledgeCategory[]>>;

  listFeatured: (
    limit?: number,
  ) => Promise<SdkResult<readonly KnowledgeArticle[]>>;

  getRelated: (
    slug: string,
    limit?: number,
  ) => Promise<SdkResult<readonly KnowledgeArticle[]>>;

  search: (input: {
    query: string;
    kind?: KnowledgeArticle['kind'];
    categorySlug?: string;
  }) => Promise<SdkResult<readonly KnowledgeSearchHit[]>>;
};

export {
  getAdjacentKnowledge,
  getCategoryBySlug,
  getCategoryIntro,
  getFeaturedKnowledge,
  getKnowledgeByCategorySlug,
  getKnowledgeByKind,
  getKnowledgeItemById,
  getKnowledgeItemBySlug,
  getRelatedKnowledge,
  knowledgeCatalog,
  knowledgeCategories,
  knowledgeCategoryIntros,
  knowledgeKindLabels,
  searchKnowledge,
} from '@/features/knowledge/domain';
export { getKnowledgeDetail, getAllKnowledgeDetailSlugs } from '@/features/knowledge/detail/registry';
export type { KnowledgeDetailContent, KnowledgeBodySection } from '@/features/knowledge/detail/types';
export { KnowledgeHubPage } from '@/features/knowledge/templates/hub-page';
export { KnowledgeListPage } from '@/features/knowledge/templates/knowledge-list-page';
export { ArticleDetailTemplate } from '@/features/knowledge/templates/article-detail-template';
export { DocsDetailTemplate } from '@/features/knowledge/templates/docs-detail-template';
export {
  KnowledgeSearchPage,
  KnowledgeSearchView,
  type KnowledgeSearchPageProps,
} from '@/features/knowledge/templates/search-page';
export { buildArticleJsonLd, knowledgeHubSeo } from '@/features/knowledge/seo';
export {
  KnowledgeEditorialTemplate,
  blogEditorialContent,
  insightsEditorialContent,
  docsEditorialContent,
  libraryEditorialContent,
  type KnowledgeEditorialContent,
} from '@/features/knowledge/editorial';

export {
  getAvailableIndustries,
  getIndustryBySlug,
  getPlannedIndustries,
  getRecommendedProductsForIndustry,
  industryCatalog,
} from '@/features/industries/domain';
export type {
  IndustryBenefit,
  IndustryChallenge,
  IndustryDefinition,
  IndustryStatus,
} from '@/features/industries/domain';
export { IndustryTemplate } from '@/features/industries/detail/industry-template';
export type { IndustryTemplateProps } from '@/features/industries/detail/industry-template';
export type { IndustryDetailContent, IndustryDetailSeo } from '@/features/industries/detail/types';
export { createFutureIndustryDetail } from '@/features/industries/detail/future-industry';
export {
  getAllIndustryDetailSlugs,
  getIndexedIndustryDetailSlugs,
  getIndustryDetail,
} from '@/features/industries/detail/registry';
export { healthcareDetailContent } from '@/features/industries/detail/content/healthcare';
export { governmentDetailContent } from '@/features/industries/detail/content/government';
export { educationDetailContent } from '@/features/industries/detail/content/education';
export { enterpriseDetailContent } from '@/features/industries/detail/content/enterprise';
export { SolutionsOverviewPage } from '@/features/industries/solutions-overview-page';
export {
  buildIndustryCatalogSchema,
  buildIndustryPageSchema,
  getIndustrySitemapPaths,
  solutionsSeo,
} from '@/features/industries/seo';

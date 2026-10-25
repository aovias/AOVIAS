export {
  getAvailableProducts,
  getProductById,
  getProductBySlug,
  getProductsByCategory,
  getProductsForGroup,
  getRoadmapProducts,
  productCatalog,
  productCategories,
  productGroups,
} from '@/features/products/domain';
export type {
  ProductCategory,
  ProductCategoryMeta,
  ProductDefinition,
  ProductEntryPoint,
  ProductGroupDefinition,
  ProductIndustry,
  ProductStatus,
} from '@/features/products/domain';
export {
  ProductDetailTemplate,
  ProductLanding,
} from '@/features/products/detail/product-detail-template';
export type { ProductDetailTemplateProps } from '@/features/products/detail/product-detail-template';
export type {
  ProductDetailContent,
  ProductDetailSeo,
  ProductFeatureShowcaseItem,
  ProductLandingContent,
} from '@/features/products/detail/types';
export { commandDetailContent } from '@/features/products/detail/content/command';
export { studioDetailContent } from '@/features/products/detail/content/studio';
export { commandLandingContent } from '@/features/products/landing/samples/command';
export { studioLandingContent } from '@/features/products/landing/samples/studio';
export { createFutureProductDetail } from '@/features/products/detail/future-product';
export {
  getAllProductDetailSlugs,
  getIndexedProductDetailSlugs,
  getProductDetail,
  resolveProductDetailPath,
} from '@/features/products/detail/registry';
export { ProductsOverviewPage } from '@/features/products/products-overview-page';
export {
  buildProductCatalogSchema,
  buildProductSoftwareSchema,
  productsSeo,
} from '@/features/products/seo';

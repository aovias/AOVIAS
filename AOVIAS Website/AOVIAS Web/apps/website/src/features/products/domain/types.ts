import type { FeatureHighlight, ProductCta } from '@/components/product';

/**
 * Shared Product domain model — configuration drives catalog and future pages.
 * Add products by extending the catalog, not by changing page structure.
 */

export type ProductStatus = 'available' | 'preview' | 'planned';

export type ProductCategory = 'platform' | 'intelligence' | 'operations';

export type ProductIndustry =
  | 'healthcare'
  | 'government'
  | 'education'
  | 'enterprise';

export type ProductEntryPoint = {
  label: string;
  href: string;
  external?: boolean;
};

export type ProductDefinition = {
  id: string;
  name: string;
  slug: string;
  category: ProductCategory;
  description: string;
  /** Short summary for cards and navigation. */
  summary: string;
  status: ProductStatus;
  /** Ordered feature bullets for catalog presentation. */
  featureSummary: readonly string[];
  industries: readonly ProductIndustry[];
  pricingEntry?: ProductEntryPoint;
  documentationEntry?: ProductEntryPoint;
  demoAvailable: boolean;
  demoCta?: ProductCta;
  highlights?: readonly FeatureHighlight[];
  /** In-page or future detail href. Prefer `#slug` until detail pages ship. */
  href: string;
  /** Sort order within catalog (lower first). */
  order: number;
  /** When true, appears in Future Products section. */
  roadmap?: boolean;
};

export type ProductCategoryMeta = {
  id: ProductCategory;
  label: string;
  description: string;
};

export type ProductGroupDefinition = {
  id: string;
  label: string;
  description: string;
  category: ProductCategory;
  productIds: readonly string[];
};

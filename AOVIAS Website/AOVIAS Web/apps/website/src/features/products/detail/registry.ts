import {
  getProductBySlug,
  getRoadmapProducts,
  productCatalog,
} from '@/features/products/domain';
import { commandDetailContent } from '@/features/products/detail/content/command';
import { platformDetailContent } from '@/features/products/detail/content/platform';
import { studioDetailContent } from '@/features/products/detail/content/studio';
import { createFutureProductDetail } from '@/features/products/detail/future-product';
import type { ProductDetailContent } from '@/features/products/detail/types';

/**
 * Product Detail Registry — maps slug → detail configuration.
 * Available products use authored content; roadmap products use the Future Product Template.
 */
const authoredDetails: Record<string, ProductDetailContent> = {
  command: commandDetailContent,
  studio: studioDetailContent,
  platform: platformDetailContent,
};

function buildRegistry(): ReadonlyMap<string, ProductDetailContent> {
  const map = new Map<string, ProductDetailContent>();

  for (const [slug, content] of Object.entries(authoredDetails)) {
    map.set(slug, content);
  }

  for (const product of getRoadmapProducts()) {
    if (!map.has(product.slug)) {
      map.set(product.slug, createFutureProductDetail(product));
    }
  }

  return map;
}

const registry = buildRegistry();

export function getProductDetail(slug: string): ProductDetailContent | undefined {
  return registry.get(slug);
}

export function getAllProductDetailSlugs(): string[] {
  return productCatalog.map((product) => product.slug);
}

export function getIndexedProductDetailSlugs(): string[] {
  return productCatalog
    .filter((product) => product.status === 'available' || product.status === 'preview')
    .map((product) => product.slug);
}

export function resolveProductDetailPath(slug: string): string {
  return `/products/${slug}`;
}

/** Ensure catalog hrefs stay aligned with detail routes. */
export function getCatalogAlignedHref(slug: string): string {
  const product = getProductBySlug(slug);
  if (!product) return '/products';
  return resolveProductDetailPath(product.slug);
}

export { authoredDetails };

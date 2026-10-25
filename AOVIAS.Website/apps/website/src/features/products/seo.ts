import { productCatalog } from '@/features/products/domain';
import type { ProductDetailContent } from '@/features/products/detail/types';
import { buildCanonicalUrl } from '@/lib/seo/canonical';
import {
  buildOrganizationReference,
  type JsonLd,
} from '@/lib/seo/schema';

/**
 * Products overview SEO — owned by the Products feature.
 */
export const productsSeo = {
  title: 'Products — Command, Studio & Platform Direction',
  description:
    'AOVIAS Command for institutional operations, AOVIAS Studio for delivery, and a coherent platform direction for what comes next.',
  path: '/products',
  keywords: [
    'AOVIAS products',
    'AOVIAS Command',
    'AOVIAS Studio',
    'enterprise platform',
    'institutional technology',
    'product portfolio',
  ],
} as const;

/**
 * ItemList of catalog products for product-aware structured data.
 */
export function buildProductCatalogSchema(): JsonLd {
  const items = productCatalog
    .filter((product) => product.status !== 'planned')
    .map((product, index) => ({
      '@type': 'ListItem',
      position: index + 1,
      name: product.name,
      description: product.summary,
      url: buildCanonicalUrl(product.href),
    }));

  return {
    '@context': 'https://schema.org',
    '@type': 'ItemList',
    name: 'AOVIAS product portfolio',
    itemListElement: items,
  };
}

/**
 * SoftwareApplication JSON-LD for product detail pages.
 */
export function buildProductSoftwareSchema(content: ProductDetailContent): JsonLd {
  return {
    '@context': 'https://schema.org',
    '@type': 'SoftwareApplication',
    name: content.name,
    description: content.seo.description,
    url: buildCanonicalUrl(`/products/${content.slug}`),
    applicationCategory: 'BusinessApplication',
    operatingSystem: 'Web',
    offers:
      content.status === 'planned'
        ? {
            '@type': 'Offer',
            availability: 'https://schema.org/PreOrder',
            description: 'Vision product — not commercially available',
          }
        : {
            '@type': 'Offer',
            url: buildCanonicalUrl(content.pricingEntry.primaryCta.href),
            description: 'Enterprise packaging — see pricing entry',
          },
    provider: buildOrganizationReference(),
  };
}

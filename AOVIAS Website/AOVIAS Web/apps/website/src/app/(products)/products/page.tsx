import { JsonLdScript } from '@/components/seo';
import {
  buildProductCatalogSchema,
  ProductsOverviewPage,
  productsSeo,
} from '@/features/products';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';
import { ogImages } from '@/seo';

export const metadata = createPageMetadata({
  title: productsSeo.title,
  description: productsSeo.description,
  path: productsSeo.path,
  type: 'product',
  keywords: productsSeo.keywords,
  image: ogImages.products,
  imageAlt: 'AOVIAS Products',
});

/**
 * AOVIAS Products — portfolio overview and landing framework entry.
 * Layout: ProductLayout → AppShell (header/footer/breadcrumbs).
 */
export default function ProductsRoutePage() {
  return (
    <>
      <JsonLdScript
        id="products-webpage"
        data={[
          ...buildPageSchemas({
            title: productsSeo.title,
            description: productsSeo.description,
            path: productsSeo.path,
            pageType: 'CollectionPage',
            includeOrganization: true,
            breadcrumbs: [
              { label: 'Home', href: '/' },
              { label: 'Products', href: productsSeo.path, current: true },
            ],
          }),
          buildProductCatalogSchema(),
        ]}
      />
      <ProductsOverviewPage />
    </>
  );
}

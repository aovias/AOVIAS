# SEO Builder API

Typed metadata API for future pages. Import from `@/lib/seo`.

```ts
import { createPageMetadata } from '@/lib/seo';

export const metadata = createPageMetadata({
  title: 'AOVIAS Studio',
  description: 'Product capability and delivery workspace.',
  path: '/products/studio',
  type: 'product',
});
```

## What the builder applies

- Site defaults (`config/seo.ts`)
- Canonical URL (`buildCanonicalUrl`)
- Open Graph + Twitter/X cards
- Robots (respects non-production → noindex)
- Icons + theme-color
- Title template via root metadata (`%s | AOVIAS`)

## Page types

`website` | `article` | `product` | `profile` | `documentation` | `legal` | `marketing` | `company`

## Structured data

```ts
import { JsonLdScript } from '@/components/seo';
import { buildPageSchemas, buildDefaultSiteSchemas } from '@/lib/seo';

// Root already emits Organization + WebSite.
// Per-page (when content exists):
<JsonLdScript data={buildPageSchemas({ title, description, path, breadcrumbs })} />
```

## Sitemap registration

Add public routes to `seoConfig.sitemap.routes` in `config/seo.ts`. Preview and `/dev` stay excluded.

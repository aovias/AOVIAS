import type { Metadata } from 'next';
import Link from 'next/link';

import {
  Container,
  Paragraph,
  Section,
  Stack,
  Text,
} from '@/components/primitives';
import {
  ProductDetailTemplate,
  studioDetailContent,
} from '@/features/products';

export const metadata: Metadata = {
  title: 'Product Detail Framework — Design System',
  robots: { index: false, follow: false },
};

/**
 * Internal preview of the Product Detail Framework using Studio configuration.
 * Route: /internal/design-system/product-landing
 */
export default function ProductLandingFrameworkPage() {
  return (
    <>
      <Container width="wide">
        <Section spacing="md">
          <Stack gap={3}>
            <Text as="p" size="small" tone="muted">
              <Link
                href="/internal/design-system/product"
                className="text-primary underline-offset-4 hover:underline"
              >
                ← Product components
              </Link>
              {' · '}
              <Link
                href="/products/studio"
                className="text-primary underline-offset-4 hover:underline"
              >
                Production Studio page
              </Link>
            </Text>
            <Text as="p" weight="semibold" size="small">
              Product Detail Framework preview
            </Text>
            <Paragraph tone="muted">
              Sprint 3.4 — reusable template for every product page. Sample uses AOVIAS
              Studio configuration. The ProductHero below provides the page h1.
            </Paragraph>
          </Stack>
        </Section>
      </Container>
      <ProductDetailTemplate content={studioDetailContent} />
    </>
  );
}

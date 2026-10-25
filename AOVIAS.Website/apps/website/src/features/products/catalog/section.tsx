import { Layers, Shield } from 'lucide-react';

import { CommandOperations, ProductPair, StudioDelivery } from '@/components/brand';
import { MarketingReveal } from '@/components/marketing/reveal';
import { FeatureCard, FeatureGrid, SectionHeader, SectionSurface } from '@/components/marketing';
import type { CardAccent } from '@/components/primitives';
import { Stack, Text } from '@/components/primitives';
import { TrustBadge } from '@/components/trust';
import type { TrustSignal } from '@/components/trust';
import { getAvailableProducts } from '@/features/products/domain';

const productIcons = {
  command: Shield,
  studio: Layers,
} as const;

/** Product identity per Milestone 7 — Command→Blue, Studio→Purple, Platform→Indigo. */
const productAccents: Record<string, CardAccent> = {
  command: 'blue',
  studio: 'purple',
  platform: 'indigo',
};

const statusSignals: Record<string, TrustSignal> = {
  available: {
    id: 'available',
    label: 'Available',
    description: 'In the current portfolio',
    variant: 'quality',
  },
  preview: {
    id: 'preview',
    label: 'Preview',
    description: 'Early access presentation',
    variant: 'default',
  },
};

/**
 * Product Catalog — configuration-driven current products with product visuals.
 */
export function ProductCatalogSection() {
  const products = getAvailableProducts();

  return (
    <SectionSurface
      id="catalog"
      tone="muted"
      bordered
      spacing="lg"
      aria-label="Product catalog"
    >
      <Stack gap={10}>
        <div className="grid items-start gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)] xl:gap-14">
          <SectionHeader
            eyebrow="Catalog"
            heading="Current products"
            description="Purpose-built platforms for institutional work. New products join this catalog through configuration — not page redesign."
          />
          <MarketingReveal>
            <ProductPair className="max-md:hidden" />
          </MarketingReveal>
        </div>
        <FeatureGrid cols={2} stagger>
          {products.map((product) => {
            const Icon =
              productIcons[product.id as keyof typeof productIcons] ?? Layers;
            const signal = statusSignals[product.status];
            const visual =
              product.id === 'command' ? (
                <CommandOperations
                  title={product.name}
                  className="max-lg:hidden"
                />
              ) : product.id === 'studio' ? (
                <StudioDelivery title={product.name} className="max-lg:hidden" />
              ) : null;
            return (
              <article
                key={product.id}
                id={product.slug}
                className="flex h-full flex-col gap-4"
              >
                <div className="flex flex-wrap items-center gap-2">
                  {signal ? <TrustBadge signal={signal} size="sm" /> : null}
                  <Text as="p" size="caption" tone="muted">
                    {product.category}
                  </Text>
                </div>
                {visual}
                <FeatureCard
                  title={product.name}
                  description={product.description}
                  icon={<Icon aria-hidden className="size-5" />}
                  href={product.href}
                  linkLabel={`Explore ${product.name}`}
                  accent={
                    productAccents[product.id as keyof typeof productAccents] ??
                    'blue'
                  }
                />
                <ul className="list-disc space-y-1 px-1 pl-5">
                  {product.featureSummary.map((feature) => (
                    <li key={feature}>
                      <Text as="span" size="small" tone="muted">
                        {feature}
                      </Text>
                    </li>
                  ))}
                </ul>
              </article>
            );
          })}
        </FeatureGrid>
      </Stack>
    </SectionSurface>
  );
}

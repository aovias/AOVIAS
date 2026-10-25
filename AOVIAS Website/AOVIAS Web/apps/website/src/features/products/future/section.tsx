import {
  FeatureCard,
  FeatureGrid,
  SectionHeader,
} from '@/components/marketing';
import { Container, Paragraph, Stack, Text } from '@/components/primitives';
import { TrustBadge } from '@/components/trust';
import type { TrustSignal } from '@/components/trust';
import { getRoadmapProducts } from '@/features/products/domain';

const plannedSignal: TrustSignal = {
  id: 'planned',
  label: 'Vision',
  description: 'Long-term platform direction',
  variant: 'default',
};

/**
 * Future Products — roadmap vision without release dates.
 */
export function FutureProductsSection() {
  const products = getRoadmapProducts();

  return (
    <section
      id="future"
      className="bg-subtle/40 w-full py-12 lg:py-16"
      aria-label="Future products"
    >
      <Container width="wide">
        <Stack gap={10}>
          <Stack gap={4} className="max-w-3xl">
            <SectionHeader
              eyebrow="Roadmap"
              heading="The long-term product vision"
              description="These capabilities describe where the ecosystem is headed. They communicate direction — not availability or schedules."
            />
            <Paragraph tone="muted">
              As products mature from vision to delivery, they move into the catalog
              through configuration. The page structure stays the same.
            </Paragraph>
          </Stack>
          <FeatureGrid cols={2} stagger>
            {products.map((product) => (
              <div key={product.id} className="flex h-full flex-col gap-3">
                <div className="flex items-center gap-2">
                  <TrustBadge signal={plannedSignal} size="sm" />
                  <Text as="span" size="caption" tone="muted">
                    {product.category}
                  </Text>
                </div>
                <FeatureCard
                  title={product.name}
                  description={product.description}
                  href={product.href}
                  linkLabel={`View ${product.name} vision`}
                />
              </div>
            ))}
          </FeatureGrid>
        </Stack>
      </Container>
    </section>
  );
}

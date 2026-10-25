import { SectionHeader } from '@/components/marketing';
import {
  Container,
  Link,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import {
  getAvailableProducts,
  getRoadmapProducts,
  productCategories,
  productGroups,
} from '@/features/products/domain';

/**
 * Product Navigation — categories, groups, and product detail links.
 * Sticky active section nav is composed at the page level via ProductNavigation.
 */
export function ProductNavigationSection() {
  const available = getAvailableProducts();
  const roadmap = getRoadmapProducts();

  return (
    <section
      id="browse"
      className="w-full py-12 lg:py-16"
      aria-label="Browse products"
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow="Navigation"
            heading="Browse the portfolio"
            description="Move by category, product group, or direct product link. Active section tracking stays with you as you scroll."
          />

          <div className="grid gap-8 lg:grid-cols-3">
            <Stack gap={3}>
              <Text as="p" weight="semibold" size="small">
                Categories
              </Text>
              <ul className="space-y-3">
                {productCategories.map((category) => (
                  <li key={category.id}>
                    <Text as="p" weight="medium" size="small">
                      {category.label}
                    </Text>
                    <Paragraph size="small" tone="muted">
                      {category.description}
                    </Paragraph>
                  </li>
                ))}
              </ul>
            </Stack>

            <Stack gap={3}>
              <Text as="p" weight="semibold" size="small">
                Product groups
              </Text>
              <ul className="space-y-3">
                {productGroups.map((group) => (
                  <li key={group.id}>
                    <Text as="p" weight="medium" size="small">
                      {group.label}
                    </Text>
                    <Paragraph size="small" tone="muted">
                      {group.description}
                    </Paragraph>
                  </li>
                ))}
              </ul>
            </Stack>

            <Stack gap={3}>
              <Text as="p" weight="semibold" size="small">
                Product links
              </Text>
              <ul className="space-y-2">
                {available.map((product) => (
                  <li key={product.id}>
                    <Link
                      href={product.href}
                      tone="primary"
                      className="text-small font-medium"
                    >
                      {product.name}
                    </Link>
                    <Paragraph size="small" tone="muted">
                      {product.summary}
                    </Paragraph>
                  </li>
                ))}
                {roadmap.map((product) => (
                  <li key={product.id}>
                    <Link
                      href={product.href}
                      className="text-muted-foreground text-small font-medium"
                    >
                      {product.name}
                      <span className="text-caption ml-2 font-normal">
                        Roadmap
                      </span>
                    </Link>
                  </li>
                ))}
              </ul>
            </Stack>
          </div>
        </Stack>
      </Container>
    </section>
  );
}

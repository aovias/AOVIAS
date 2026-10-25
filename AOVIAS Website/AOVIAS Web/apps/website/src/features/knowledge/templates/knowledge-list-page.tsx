import { KnowledgeCover } from '@/components/brand';
import type { ContentCategory, ContentItem } from '@/components/content';
import {
  CtaSection,
  Hero,
  SectionHeader,
} from '@/components/marketing';
import { Container, Link, Stack } from '@/components/primitives';
import { CategoryBadge, ResourceGrid } from '@/components/resources';

export type KnowledgeListPageProps = {
  eyebrow: string;
  title: string;
  description: string;
  items: readonly ContentItem[];
  categories?: readonly ContentCategory[];
  activeCategorySlug?: string;
  emptyMessage?: string;
};

/**
 * Shared list template for Blog, Insights, Documentation index, Resources library, Categories.
 */
export function KnowledgeListPage({
  eyebrow,
  title,
  description,
  items,
  categories,
  activeCategorySlug,
  emptyMessage = 'No content is available in this section yet.',
}: KnowledgeListPageProps) {
  return (
    <>
      <Hero
        eyebrow={eyebrow}
        title={title}
        description={description}
        primaryCta={{ label: 'Search knowledge', href: '/resources/search' }}
        secondaryCta={{ label: 'Browse categories', href: '/resources/categories' }}
        atmosphere="knowledge-pattern"
        illustration={
          <KnowledgeCover
            label="AOVIAS Knowledge"
            tone="secondary"
            className="aspect-[4/3]"
          />
        }
      />

      <section className="w-full py-12 lg:py-16" aria-label={title}>
        <Container width="wide">
          <Stack gap={8}>
            <SectionHeader
              heading={title}
              description={`${items.length} item${items.length === 1 ? '' : 's'} in this section.`}
            />

            {categories && categories.length > 0 ? (
              <ul className="flex flex-wrap gap-2" aria-label="Filter by category">
                {categories.map((category) => (
                  <li key={category.id}>
                    <Link
                      href={`/resources/categories/${category.slug}`}
                      className="inline-flex"
                    >
                      <CategoryBadge
                        category={category}
                        active={category.slug === activeCategorySlug}
                      />
                    </Link>
                  </li>
                ))}
              </ul>
            ) : null}

            <ResourceGrid items={items} cols={3} emptyMessage={emptyMessage} />
          </Stack>
        </Container>
      </section>

      <CtaSection
        title="Need something specific?"
        description="Search AOVIAS Knowledge or explore products and industries."
        primaryCta={{ label: 'Open search', href: '/resources/search' }}
        secondaryCta={{ label: 'Explore products', href: '/products' }}
        background="dark"
      />
    </>
  );
}

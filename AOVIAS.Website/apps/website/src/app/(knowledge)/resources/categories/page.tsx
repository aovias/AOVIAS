import { KnowledgeCover } from '@/components/brand';
import { JsonLdScript } from '@/components/seo';
import {
  CtaSection,
  FeatureCard,
  FeatureGrid,
  Hero,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import {
  getKnowledgeByCategorySlug,
  knowledgeCategories,
} from '@/features/knowledge';
import { buildPageSchemas, createPageMetadata } from '@/lib/seo';

const path = '/resources/categories';

export const metadata = createPageMetadata({
  title: 'Categories',
  description:
    'Browse AOVIAS knowledge by category — engineering, platform, governance, operations, guides, and documentation.',
  path,
  type: 'marketing',
});

export default function CategoriesIndexPage() {
  return (
    <>
      <JsonLdScript
        id="categories-index"
        data={buildPageSchemas({
          title: 'Categories',
          description: 'Knowledge categories',
          path,
          breadcrumbs: [
            { label: 'Home', href: '/' },
            { label: 'Resources', href: '/resources' },
            { label: 'Categories', href: path, current: true },
          ],
        })}
      />
      <Hero
        eyebrow="Categories"
        title="Browse the knowledge library by topic"
        description="Platform, engineering, governance, operations, product guides, and documentation — category identities for discovery, not tag clouds."
        primaryCta={{ label: 'Search', href: '/resources/search' }}
        secondaryCta={{ label: 'Knowledge hub', href: '/resources' }}
        atmosphere="knowledge-pattern"
        illustration={
          <KnowledgeCover
            label="Category identity"
            tone="info"
            motif="architecture"
            className="aspect-[4/3]"
          />
        }
      />
      <section className="w-full py-12 lg:py-16" aria-label="Knowledge categories">
        <Container width="wide">
          <Stack gap={10}>
            <SectionHeader
              heading="All categories"
              description="Open a topic to browse related writing and guidance."
            />
            <FeatureGrid cols={3} stagger>
              {knowledgeCategories.map((category) => {
                const count = getKnowledgeByCategorySlug(category.slug ?? category.id)
                  .length;
                return (
                  <FeatureCard
                    key={category.id}
                    title={category.name}
                    description={`${count} item${count === 1 ? '' : 's'} in this category.`}
                    href={`/resources/categories/${category.slug}`}
                    linkLabel={`Open ${category.name}`}
                  />
                );
              })}
            </FeatureGrid>
          </Stack>
        </Container>
      </section>
      <CtaSection
        title="Prefer search?"
        description="Query across every knowledge surface with filters for type and category."
        primaryCta={{ label: 'Open search', href: '/resources/search' }}
        background="dark"
      />
    </>
  );
}

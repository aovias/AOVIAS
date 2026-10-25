import Link from 'next/link';

import { KnowledgeCover } from '@/components/brand';
import {
  CtaSection,
  FeatureCard,
  FeatureGrid,
  Hero,
  SectionHeader,
} from '@/components/marketing';
import { ArticleCard } from '@/components/resources/article-card';
import { Stack, Text } from '@/components/primitives';
import { EditorialMagazineGrid } from '@/features/knowledge/editorial/editorial-magazine-grid';
import type { KnowledgeEditorialContent } from '@/features/knowledge/editorial/types';
import {
  getFeaturedKnowledge,
  getKnowledgeByKind,
} from '@/features/knowledge/domain';
import { TechnologyStoryFlow } from '@/features/technology/editorial/technology-story-flow';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export type KnowledgeEditorialTemplateProps = {
  content: KnowledgeEditorialContent;
};

/**
 * Knowledge section editorial hub — Enterprise Knowledge Platform (Phase V7).
 * Hero → Orient → Category library → Featured → Magazine grid → Learning path → CTA
 */
export function KnowledgeEditorialTemplate({
  content,
}: KnowledgeEditorialTemplateProps) {
  const c = content;
  const items = getKnowledgeByKind([...c.kinds]).sort((a, b) => {
    const aTime = a.publishedAt ? Date.parse(a.publishedAt) : 0;
    const bTime = b.publishedAt ? Date.parse(b.publishedAt) : 0;
    return bTime - aTime;
  });
  const featured =
    items.find((item) => item.featured) ??
    getFeaturedKnowledge().find((item) => c.kinds.includes(item.kind)) ??
    items[0];
  const gridItems = featured
    ? [featured, ...items.filter((item) => item.id !== featured.id)]
    : items;

  return (
    <>
      <Hero
        atmosphere={c.atmosphere}
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        {...(c.hero.secondaryCta ? { secondaryCta: c.hero.secondaryCta } : {})}
        illustration={
          c.illustration ?? (
            <KnowledgeCover
              label={c.cover.label}
              tone={c.cover.tone ?? 'secondary'}
              motif={c.cover.motif ?? 'default'}
              className="aspect-[4/3] xl:min-h-[18rem]"
            />
          )
        }
      />

      <PlatformSectionBand
        id="orient"
        eyebrow={c.orient.eyebrow ?? 'Purpose'}
        title={c.orient.title}
        description={c.orient.description}
        tone="feature"
      />

      <PlatformSectionBand
        id="library"
        eyebrow={c.libraryNav.eyebrow ?? 'Library'}
        title={c.libraryNav.title}
        description={c.libraryNav.description}
        muted
      >
        <ul className="flex flex-wrap gap-2 sm:gap-3" aria-label="Knowledge categories">
          {c.libraryNav.categories.map((category) => (
            <li key={category.id}>
              <Link
                href={category.href}
                className="border-border bg-card/70 text-foreground hover:border-domain-accent/40 hover:bg-card focus-visible:ring-focus-ring inline-flex rounded-md border px-3 py-1.5 text-small font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none"
              >
                {category.name}
              </Link>
            </li>
          ))}
        </ul>
      </PlatformSectionBand>

      {featured ? (
        <PlatformSectionBand
          id="featured"
          eyebrow={c.featured.eyebrow ?? 'Featured'}
          title={c.featured.title}
          description={c.featured.description}
          tone="feature"
        >
          <div className="mx-auto max-w-3xl">
            <ArticleCard item={featured} featured />
            <Text as="p" size="small" tone="muted" className="mt-3">
              {[
                featured.readingTimeMinutes
                  ? `${featured.readingTimeMinutes} min read`
                  : null,
                featured.category?.name,
                featured.kind.replaceAll('-', ' '),
              ]
                .filter(Boolean)
                .join(' · ')}
            </Text>
          </div>
        </PlatformSectionBand>
      ) : null}

      <PlatformSectionBand
        id="editorial"
        eyebrow={c.grid.eyebrow ?? 'Editorial'}
        title={c.grid.title}
        description={c.grid.description}
        muted
      >
        <EditorialMagazineGrid items={gridItems} />
      </PlatformSectionBand>

      <PlatformSectionBand
        id="path"
        eyebrow="Learning path"
        title={c.learningPath.title}
        description={c.learningPath.description}
        tone="feature"
      >
        <div className="grid items-center gap-10 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.05fr)] xl:gap-14">
          <Stack gap={4}>
            <SectionHeader
              heading="A guided journey through AOVIAS Knowledge"
              description="Structured exploration — not isolated reading."
            />
            <FeatureGrid cols={2} stagger={false}>
              <FeatureCard
                title="Continue learning"
                description="Browse related categories and deepen operational understanding."
                href="/resources/categories"
                linkLabel="Browse categories"
              />
              <FeatureCard
                title="Search the library"
                description="Find guides, architecture notes, and documentation by topic."
                href="/resources/search"
                linkLabel="Open search"
              />
            </FeatureGrid>
          </Stack>
          <TechnologyStoryFlow
            title={c.learningPath.title}
            steps={c.learningPath.steps}
            caption="Knowledge journey"
          />
        </div>
      </PlatformSectionBand>

      <div id="cta">
        <CtaSection
          title={c.closing.title}
          description={c.closing.description}
          primaryCta={c.closing.primaryCta}
          {...(c.closing.secondaryCta
            ? { secondaryCta: c.closing.secondaryCta }
            : {})}
          background="dark"
        />
      </div>
    </>
  );
}

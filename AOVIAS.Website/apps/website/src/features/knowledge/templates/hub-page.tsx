import { KnowledgeCover } from '@/components/brand';
import {
  CtaSection,
  FeatureCard,
  FeatureGrid,
  Hero,
  SectionHeader,
} from '@/components/marketing';
import { Stack, Text } from '@/components/primitives';
import { EditorialMagazineGrid } from '@/features/knowledge/editorial/editorial-magazine-grid';
import { knowledgeLibraryCategories } from '@/features/knowledge/editorial/content/sections';
import {
  getFeaturedKnowledge,
  getKnowledgeByKind,
  knowledgeCategories,
} from '@/features/knowledge/domain';
import { TechnologyStoryFlow } from '@/features/technology/editorial/technology-story-flow';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';
import Link from 'next/link';

/**
 * Knowledge hub — Enterprise Knowledge Platform entry (Phase V7).
 */
export function KnowledgeHubPage() {
  const featured = getFeaturedKnowledge();
  const recent = [
    ...getKnowledgeByKind(['article']),
    ...getKnowledgeByKind(['insight']),
    ...getKnowledgeByKind(['documentation']),
    ...getKnowledgeByKind(['resource', 'release-note']),
  ]
    .sort((a, b) => {
      const aTime = a.publishedAt ? Date.parse(a.publishedAt) : 0;
      const bTime = b.publishedAt ? Date.parse(b.publishedAt) : 0;
      return bTime - aTime;
    })
    .slice(0, 7);

  const sections = [
    {
      id: 'blog',
      title: 'Blog',
      description: 'Thought leadership and engineering culture.',
      href: '/resources/blog',
    },
    {
      id: 'insights',
      title: 'Insights',
      description: 'Research and executive perspectives.',
      href: '/resources/insights',
    },
    {
      id: 'docs',
      title: 'Documentation',
      description: 'Structured product and architecture guidance.',
      href: '/resources/docs',
    },
    {
      id: 'library',
      title: 'Resources',
      description: 'Guides, checklists, and reference material.',
      href: '/resources/library',
    },
  ] as const;

  return (
    <>
      <Hero
        eyebrow="Knowledge"
        title="An enterprise knowledge platform."
        description="Architecture, engineering, governance, AI, operations, and industry insight — treated as a product, not a blog feed."
        primaryCta={{ label: 'Continue learning', href: '/resources/blog' }}
        secondaryCta={{ label: 'Explore documentation', href: '/resources/docs' }}
        atmosphere="knowledge-pattern"
        illustration={
          <KnowledgeCover
            label="Blog · Insights · Docs · Resources"
            tone="secondary"
            motif="architecture"
            className="aspect-[4/3] xl:min-h-[18rem]"
          />
        }
      />

      <PlatformSectionBand
        id="library"
        eyebrow="Knowledge library"
        title="Four experiences. One visual language."
        description="Each surface has a distinct purpose while sharing editorial standards, search, and discovery architecture."
        tone="feature"
      >
        <FeatureGrid cols={2} stagger>
          {sections.map((section) => (
            <FeatureCard
              key={section.id}
              title={section.title}
              description={section.description}
              href={section.href}
              linkLabel={`Open ${section.title}`}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="categories"
        eyebrow="Categories"
        title="Navigate as a library"
        description="Topic identities — not endless tag clouds."
        muted
      >
        <ul className="flex flex-wrap gap-2 sm:gap-3" aria-label="Knowledge categories">
          {knowledgeLibraryCategories.map((category) => (
            <li key={category.id}>
              <Link
                href={category.href}
                className="border-border bg-card/70 text-foreground hover:border-domain-accent/40 inline-flex rounded-md border px-3 py-1.5 text-small font-medium transition-colors"
              >
                {category.name}
              </Link>
            </li>
          ))}
        </ul>
        <Text as="p" size="small" tone="muted" className="mt-4">
          {knowledgeCategories.length} indexed topics power filters, category pages, and
          related discovery — ready for Release 2 full-text search.
        </Text>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="featured"
        eyebrow="Featured"
        title="Flagship knowledge"
        description="Editorial covers for deep reading — not promotional tiles."
        tone="feature"
      >
        <EditorialMagazineGrid items={featured.length > 0 ? featured : recent} />
      </PlatformSectionBand>

      <PlatformSectionBand
        id="path"
        eyebrow="Learning path"
        title="Understanding AOVIAS"
        description="A structured journey through the knowledge platform."
        muted
      >
        <div className="grid items-center gap-10 lg:grid-cols-2 xl:gap-14">
          <Stack gap={3}>
            <SectionHeader
              heading="Explore with intention"
              description="From fundamentals to enterprise scale — guided, not trapped."
            />
          </Stack>
          <TechnologyStoryFlow
            title="AOVIAS knowledge journey"
            steps={[
              'Understanding AOVIAS',
              'Platform fundamentals',
              'Operations',
              'Governance',
              'Deployment',
              'Enterprise scale',
            ]}
            caption="Learning path"
          />
        </div>
      </PlatformSectionBand>

      <CtaSection
        title="Continue learning"
        description="Read documentation, request a demonstration, or search the full knowledge library."
        primaryCta={{ label: 'Explore documentation', href: '/resources/docs' }}
        secondaryCta={{
          label: 'Request a demo',
          href: '/company/contact#form-demo',
        }}
        background="dark"
      />
    </>
  );
}

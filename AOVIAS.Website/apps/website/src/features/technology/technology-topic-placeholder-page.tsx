import { SystemEvolution } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import {
  getRelatedTechnologyTopics,
  getTechnologyTopicBySlug,
  getTechnologyTopicHref,
  type TechnologyTopic,
} from '@/features/technology/domain';
import { getNavIcon } from '@/lib/navigation/icons';

export type TechnologyTopicPlaceholderPageProps = {
  slug: string;
};

/**
 * Shared placeholder for Technology child routes still awaiting full bodies.
 * Uses enterprise hero language — not dashed illustration placeholders.
 */
export function TechnologyTopicPlaceholderPage({
  slug,
}: TechnologyTopicPlaceholderPageProps) {
  const topic = getTechnologyTopicBySlug(slug);
  const related = getRelatedTechnologyTopics(slug, 4);
  const title = topic?.label ?? 'Technology topic';
  const description =
    topic?.description ??
    'This Technology topic is registered in the site information architecture and will be published in a later release.';

  const statusNote =
    topic?.status === 'future'
      ? 'This surface is planned. Navigation and routing are in place so the section can grow without restructuring.'
      : 'A concise overview will be published here. Until then, explore related Technology topics or contact AOVIAS.';

  return (
    <>
      <Hero
        eyebrow="Technology"
        title={title}
        description={`${description} ${statusNote}`}
        primaryCta={{ label: 'Technology overview', href: '/technology' }}
        secondaryCta={{ label: 'Contact AOVIAS', href: '/company/contact' }}
        atmosphere="technology-atmosphere"
        illustration={
          <SystemEvolution title={`${title} — measured platform evolution`} />
        }
      />

      {related.length > 0 ? (
        <section
          className="border-border w-full border-t py-14 md:py-16"
          aria-labelledby="related-technology-heading"
        >
          <Container width="wide">
            <Stack gap={8}>
              <div id="related-technology-heading">
                <SectionHeader
                  eyebrow="Related"
                  heading="Related technology topics"
                  description="Continue through the Technology section information architecture."
                  align="start"
                />
              </div>
              <FeatureGrid cols={2} stagger>
                {related.map((item: TechnologyTopic) => {
                  const Icon = getNavIcon(item.icon);
                  return (
                    <FeatureCard
                      key={item.id}
                      title={item.label}
                      description={item.description}
                      href={getTechnologyTopicHref(item)}
                      linkLabel="View topic"
                      {...(Icon
                        ? {
                            icon: (
                              <Icon className="size-5" aria-hidden="true" />
                            ),
                          }
                        : {})}
                    />
                  );
                })}
              </FeatureGrid>
            </Stack>
          </Container>
        </section>
      ) : null}
    </>
  );
}

import { FeatureGrid, SectionHeader, ValueCard } from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { whyContent, whyMeta } from '@/features/careers/why-aovias/content';

/**
 * Why AOVIAS — career rationale without hype.
 */
export function WhyAoviasSection() {
  return (
    <section
      id={whyMeta.sectionId}
      className="bg-muted/20 w-full py-16 lg:py-20"
      aria-label={whyMeta.title}
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow={whyContent.eyebrow}
            heading={whyContent.heading}
            description={whyContent.description}
          />
          <FeatureGrid cols={2} stagger>
            {whyContent.reasons.map((item) => (
              <ValueCard
                key={item.id}
                heading={item.heading}
                description={item.description}
              />
            ))}
          </FeatureGrid>
        </Stack>
      </Container>
    </section>
  );
}

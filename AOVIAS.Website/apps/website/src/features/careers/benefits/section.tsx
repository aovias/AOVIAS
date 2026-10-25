import {
  FeatureCard,
  FeatureGrid,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import {
  benefitsContent,
  benefitsMeta,
} from '@/features/careers/benefits/content';

/**
 * Benefits — expandable principles of working at AOVIAS.
 */
export function BenefitsSection() {
  return (
    <section
      id={benefitsMeta.sectionId}
      className="w-full py-16 lg:py-20"
      aria-label={benefitsMeta.title}
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow={benefitsContent.eyebrow}
            heading={benefitsContent.heading}
            description={benefitsContent.description}
          />
          <FeatureGrid cols={3} stagger>
            {benefitsContent.items.map((item) => (
              <FeatureCard
                key={item.id}
                title={item.title}
                description={item.description}
              />
            ))}
          </FeatureGrid>
        </Stack>
      </Container>
    </section>
  );
}

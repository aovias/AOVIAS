import {
  FeatureCard,
  FeatureGrid,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { cultureContent, cultureMeta } from '@/features/careers/culture/content';

/**
 * Careers culture — employer-facing principles.
 */
export function CareersCultureSection() {
  return (
    <section
      id={cultureMeta.sectionId}
      className="w-full py-16 lg:py-20"
      aria-label={cultureMeta.title}
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow={cultureContent.eyebrow}
            heading={cultureContent.heading}
            description={cultureContent.description}
          />
          <FeatureGrid cols={3} stagger>
            {cultureContent.principles.map((item) => (
              <FeatureCard
                key={item.id}
                title={item.title}
                description={item.description}
              />
            ))}
          </FeatureGrid>
          <div>
            <MarketingCtaLink
              label="Read company culture"
              href={cultureContent.companyCultureHref}
              variant="outline"
              size="md"
            />
          </div>
        </Stack>
      </Container>
    </section>
  );
}

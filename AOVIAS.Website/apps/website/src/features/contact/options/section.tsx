import {
  FeatureCard,
  FeatureGrid,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { companyLegal } from '@/config/company-legal';
import { engagementOptions } from '@/features/contact/domain';
import {
  optionsContent,
  optionsMeta,
} from '@/features/contact/options/content';

/**
 * Contact options — configuration-driven engagement pathways.
 */
export function ContactOptionsSection() {
  return (
    <section
      id={optionsMeta.sectionId}
      className="w-full scroll-mt-24 py-16 lg:py-20"
      aria-label={optionsMeta.title}
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow={optionsContent.eyebrow}
            heading={optionsContent.heading}
            description={optionsContent.description}
          />
          <FeatureGrid cols={3} stagger>
            {engagementOptions.map((option) => (
              <FeatureCard
                key={option.id}
                title={option.title}
                description={`${option.description} ${option.responseExpectation}`}
                href={option.href}
                linkLabel={option.linkLabel}
              />
            ))}
          </FeatureGrid>
          <Text as="p" size="caption" tone="muted">
            Prefer email or phone? Write to{' '}
            <a
              href={`mailto:${companyLegal.enquiryEmail}`}
              className="text-primary underline underline-offset-4"
            >
              {companyLegal.enquiryEmail}
            </a>
            {' '}
            or call{' '}
            <a
              href={`tel:${companyLegal.phoneTel}`}
              className="text-primary underline underline-offset-4"
            >
              {companyLegal.phoneDisplay}
            </a>
            .
          </Text>
        </Stack>
      </Container>
    </section>
  );
}

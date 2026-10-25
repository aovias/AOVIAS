import { Section } from '@/components/layout';
import {
  FeatureCard,
  FeatureGrid,
  SectionHeader,
} from '@/components/marketing';
import { EnterpriseCta } from '@/components/pricing';
import { Stack } from '@/components/primitives';
import {
  enterpriseCapabilities,
  enterpriseOffering,
} from '@/features/pricing/domain';

/**
 * Enterprise offering — capabilities + EnterpriseCta.
 */
export function EnterprisePricingSection() {
  return (
    <Section spacing="md" id="enterprise" className="bg-subtle/40">
      <Stack gap={10}>
        <SectionHeader
          eyebrow="Enterprise"
          heading="Enterprise offering"
          description="When published list plans are not enough, AOVIAS structures commercial partnerships around institutional scope, procurement, and long-term operations."
        />
        <FeatureGrid cols={3} stagger>
          {enterpriseCapabilities.map((item) => (
            <FeatureCard
              key={item.id}
              title={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
        <EnterpriseCta offering={enterpriseOffering} background="subtle" />
      </Stack>
    </Section>
  );
}

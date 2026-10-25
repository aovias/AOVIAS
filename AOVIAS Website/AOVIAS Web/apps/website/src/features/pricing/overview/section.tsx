import { DashboardConsole } from '@/components/brand';
import { Section } from '@/components/layout';
import { MarketingReveal } from '@/components/marketing/reveal';
import { SectionHeader } from '@/components/marketing';
import { Paragraph, Stack, Text } from '@/components/primitives';

/**
 * Pricing overview — licensing philosophy, engagement, implementation, support.
 */
export function PricingOverviewSection() {
  return (
    <Section spacing="md" id="pricing-overview" className="bg-muted/55">
      <Stack gap={8}>
        <div className="grid items-start gap-10 lg:grid-cols-[minmax(0,1.15fr)_minmax(0,0.85fr)] xl:gap-14">
          <SectionHeader
            eyebrow="Overview"
            heading="Licensing philosophy"
            description="AOVIAS prices for clarity first: published list plans where they help evaluation, and enterprise engagement where institutions need tailored agreements."
          />
          <MarketingReveal>
            <DashboardConsole
              title="Transparent commercial evaluation"
              className="max-md:hidden"
            />
          </MarketingReveal>
        </div>
        <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-4">
          <Stack gap={2}>
            <Text as="p" weight="medium">
              Clear licensing
            </Text>
            <Paragraph size="small" tone="muted">
              Studio and Command are commercially available today. Plans reflect
              product purpose — delivery workspace versus operational command —
              with transparent list prices for evaluation.
            </Paragraph>
          </Stack>
          <Stack gap={2}>
            <Text as="p" weight="medium">
              Enterprise engagement
            </Text>
            <Paragraph size="small" tone="muted">
              Custom pricing, volume licensing, and procurement support are
              handled through enterprise inquiry when list plans are not the
              right fit — without countdown urgency.
            </Paragraph>
          </Stack>
          <Stack gap={2}>
            <Text as="p" weight="medium">
              Implementation approach
            </Text>
            <Paragraph size="small" tone="muted">
              Implementation services focus on structured onboarding and
              responsible rollout. Scope is agreed before work begins so
              ownership stays clear.
            </Paragraph>
          </Stack>
          <Stack gap={2}>
            <Text as="p" weight="medium">
              Support
            </Text>
            <Paragraph size="small" tone="muted">
              Published plans include standard or priority support as listed.
              Enterprise engagements can add dedicated success pathways matched
              to operational criticality.
            </Paragraph>
          </Stack>
        </div>
      </Stack>
    </Section>
  );
}

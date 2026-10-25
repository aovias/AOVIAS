import { Section } from '@/components/layout';
import { SectionHeader } from '@/components/marketing';
import { PricingFaq } from '@/components/pricing';
import { Stack } from '@/components/primitives';
import { pricingFaqItems } from '@/features/pricing/domain';

/**
 * Pricing FAQ — Trust FAQ via PricingFaq commerce wrapper.
 */
export function PricingFaqSection() {
  return (
    <Section spacing="md" id="pricing-faq" className="bg-background">
      <Stack gap={8}>
        <SectionHeader
          eyebrow="FAQ"
          heading="Pricing questions"
          description="Licensing, list prices, billing, support, implementation, evaluation, and enterprise purchasing — answered without sales pressure."
        />
        <PricingFaq items={pricingFaqItems} />
      </Stack>
    </Section>
  );
}

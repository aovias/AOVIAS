import { DashboardConsole } from '@/components/brand';
import { Hero } from '@/components/marketing';
import { PricingBanner } from '@/components/pricing';
import { Container, Stack } from '@/components/primitives';
import { pricingBanner } from '@/features/pricing/domain';

/**
 * Pricing hero — enterprise composition with platform visualization.
 */
export function PricingHeroSection() {
  return (
    <>
      <Hero
        eyebrow="Pricing"
        title="Clear commercial options for institutional software"
        description="Evaluate published Studio and Command list prices with transparent billing choices. Enterprise licensing remains available when institutions need custom scope."
        primaryCta={{ label: 'View plans', href: '#plans' }}
        secondaryCta={{ label: 'Talk with AOVIAS', href: '/company/contact' }}
        atmosphere="subtle-texture"
        illustration={
          <DashboardConsole title="Commercial clarity — evaluate with confidence" />
        }
      />
      <section
        className="border-border from-surface-muted to-background w-full border-b bg-gradient-to-b py-8"
        aria-label="Pricing note"
      >
        <Container width="wide">
          <Stack gap={4}>
            <PricingBanner message={pricingBanner} />
          </Stack>
        </Container>
      </section>
    </>
  );
}

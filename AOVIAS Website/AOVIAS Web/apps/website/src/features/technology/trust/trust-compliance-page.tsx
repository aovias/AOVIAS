import { ComplianceSupport } from '@/components/brand';
import {
  FeatureCard,
  FeatureGrid,
  Hero,
  MarketingCtaLink,
  SectionHeader,
} from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import { trustComplianceContent } from '@/features/technology/trust/content';
import { PlatformSectionBand } from '@/features/technology/platform/section-band';

export function TrustCompliancePage() {
  const c = trustComplianceContent;

  return (
    <>
      <Hero
        atmosphere="security-geometry"
        eyebrow={c.hero.eyebrow}
        title={c.hero.title}
        description={c.hero.description}
        primaryCta={c.hero.primaryCta}
        secondaryCta={c.hero.secondaryCta}
        illustration={<ComplianceSupport />}
      />

      <section className="bg-muted/40 border-border w-full border-b py-8" aria-label="Important notice">
        <Container width="wide">
          <Text as="p" size="small" tone="muted" className="max-w-3xl">
            {c.disclaimer}
          </Text>
        </Container>
      </section>

      <PlatformSectionBand
        id="compliance-topics"
        eyebrow="Readiness"
        title="How the platform supports institutional duties"
        description="We discuss design support — not certifications, approvals, or guarantees of regulatory outcome."
      >
        <FeatureGrid cols={2} stagger={false}>
          {c.topics.map((item) => (
            <FeatureCard
              key={item.title}
              title={item.title}
              description={item.description}
            />
          ))}
        </FeatureGrid>
      </PlatformSectionBand>

      <PlatformSectionBand
        id="compliance-model"
        eyebrow="Model"
        title="Industry context on shared platform controls"
        muted
      >
        <div className="mx-auto max-w-2xl">
          <ComplianceSupport />
        </div>
      </PlatformSectionBand>

      <section className="w-full py-14 md:py-16" aria-labelledby="compliance-next">
        <Container width="wide">
          <Stack gap={6} className="max-w-3xl">
            <div id="compliance-next">
              <SectionHeader
                eyebrow="Related"
                heading="Data protection and security practice"
                description="Protection principles and continuous security engineering reinforce readiness."
              />
            </div>
            <Stack direction="horizontal" gap={3} wrap>
              <MarketingCtaLink
                label="Data protection"
                href="/technology/trust/data-protection"
                variant="primary"
              />
              <MarketingCtaLink label="Security" href="/technology/trust" variant="outline" />
            </Stack>
          </Stack>
        </Container>
      </section>
    </>
  );
}

import {
  InstitutionalTrust,
  RegisteredIllustration,
} from '@/components/brand';
import {
  CtaSection,
  FeatureCard,
  FeatureGrid,
  Hero,
  HorizonReveal,
  MarketingCtaLink,
  PremiumProductCard,
  SectionHeader,
  SectionSurface,
} from '@/components/marketing';
import { HomeHeroAtmosphere } from '@/components/marketing/home-hero-atmosphere';
import { Paragraph, Stack } from '@/components/primitives';
import {
  homeArchitecture,
  homeCta,
  homeEcosystem,
  homeHero,
  homeIndustries,
  homeProducts,
  homeTechnology,
  homeTrust,
  homeWhy,
} from '@/content/home';

/**
 * Homepage — V3.1 editorial refinement.
 * Narrative: Institution → Platform → Products → Technology → Architecture → Industries → Trust → CTA
 * Scroll reveals omitted on this route (V11) to keep Framer off the mobile LCP path.
 */
export function HomeView() {
  return (
    <>
      <div className="relative overflow-x-clip" data-hero="hero-home">
        <HomeHeroAtmosphere />
        <HorizonReveal>
          <Hero
            eyebrow={homeHero.eyebrow}
            title={homeHero.title}
            description={homeHero.description}
            primaryCta={homeHero.primaryCta}
            secondaryCta={homeHero.secondaryCta}
            highlights={homeHero.highlights}
            ctaWithArrow
            titleSize="h1"
            backgroundClassName="bg-transparent"
            className="relative border-transparent"
            illustration={
              <RegisteredIllustration
                id="ill-platform-core"
                title="AOVIAS platform orchestration"
                className="xl:min-h-[20rem] 2xl:min-h-[24rem]"
              />
            }
          />
        </HorizonReveal>
      </div>

      <SectionSurface
        tone="canvas"
        atmosphere="corporate"
        aria-label={homeWhy.heading}
        spacing="md"
      >
        <div className="grid items-center gap-10 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)] lg:gap-14">
          <Stack gap={5}>
            <SectionHeader
              eyebrow={homeWhy.eyebrow}
              heading={homeWhy.heading}
            />
            {homeWhy.paragraphs.map((paragraph) => (
              <Paragraph key={paragraph.slice(0, 24)} size="body-lg" tone="muted">
                {paragraph}
              </Paragraph>
            ))}
            <MarketingCtaLink
              label={homeWhy.cta.label}
              href={homeWhy.cta.href}
              variant="soft"
              size="lg"
              withArrow
              className="mt-2 w-fit"
            />
          </Stack>
          <InstitutionalTrust title="AOVIAS institutional trust illustration" />
        </div>
      </SectionSurface>

      <SectionSurface
        tone="feature"
        atmosphere="platform"
        aria-label={homeEcosystem.heading}
        spacing="lg"
      >
        <Stack gap={10}>
          <div className="mx-auto max-w-2xl text-center">
            <SectionHeader
              eyebrow={homeEcosystem.eyebrow}
              heading={homeEcosystem.heading}
              description={homeEcosystem.description}
              align="center"
            />
          </div>

          <RegisteredIllustration
            id="ill-platform-core"
            componentKey="PlatformEcosystem"
            title="Platform → products → institutions"
            className="mx-auto w-full max-w-3xl"
          />

          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3 lg:gap-6">
            {homeProducts.items.map((item) => (
              <PremiumProductCard
                key={item.id}
                title={item.title}
                description={item.description}
                href={item.href}
                linkLabel={item.linkLabel}
                domainAttr={
                  item.id === 'platform'
                    ? { 'data-technology': 'platform' }
                    : { 'data-product': item.id }
                }
                media={
                  <RegisteredIllustration
                    id={item.illustrationId}
                    title={item.title}
                    className="min-h-[7.5rem]"
                  />
                }
              />
            ))}
          </div>
        </Stack>
      </SectionSurface>

      <SectionSurface
        tone="muted"
        bordered
        aria-label={homeTechnology.heading}
        spacing="md"
      >
        <Stack gap={8}>
          <SectionHeader
            eyebrow={homeTechnology.eyebrow}
            heading={homeTechnology.heading}
            description={homeTechnology.description}
          />
          <FeatureGrid cols={4} stagger={false}>
            {homeTechnology.items.map((item) => (
              <div key={item.id} data-technology={item.id} className="h-full">
                <FeatureCard
                  title={item.title}
                  description={item.description}
                  href={item.href}
                  linkLabel={item.linkLabel}
                  accent="slate"
                />
              </div>
            ))}
          </FeatureGrid>
        </Stack>
      </SectionSurface>

      <SectionSurface
        tone="elevated"
        atmosphere="blueprint"
        aria-label={homeArchitecture.heading}
        spacing="lg"
      >
        <div className="grid items-center gap-10 lg:grid-cols-2 lg:gap-14">
          <Stack gap={5}>
            <SectionHeader
              eyebrow={homeArchitecture.eyebrow}
              heading={homeArchitecture.heading}
              description={homeArchitecture.description}
            />
            <MarketingCtaLink
              label={homeArchitecture.cta.label}
              href={homeArchitecture.cta.href}
              variant="primary"
              className="w-fit"
            />
          </Stack>
          <RegisteredIllustration
            id="ill-architecture-layers"
            title="Platform architecture"
            className="xl:min-h-[16rem]"
          />
        </div>
      </SectionSurface>

      <SectionSurface
        tone="canvas"
        atmosphere="corporate"
        bordered
        aria-label={homeIndustries.heading}
        spacing="md"
      >
        <Stack gap={8}>
          <div className="grid items-center gap-8 lg:grid-cols-[minmax(0,0.95fr)_minmax(0,1.05fr)] lg:gap-12">
            <SectionHeader
              eyebrow={homeIndustries.eyebrow}
              heading={homeIndustries.heading}
              description={homeIndustries.description}
            />
            <RegisteredIllustration
              id="ill-company-institution"
              componentKey="IndustryPlatformComposition"
              title="Industry platforms"
              className="w-full"
            />
          </div>
          <FeatureGrid cols={4} stagger={false}>
            {homeIndustries.items.map((item) => (
              <FeatureCard
                key={item.id}
                title={item.title}
                description={item.description}
                href={item.href}
                linkLabel={item.linkLabel}
              />
            ))}
          </FeatureGrid>
        </Stack>
      </SectionSurface>

      <SectionSurface
        tone="contrast"
        atmosphere="security"
        aria-label={homeTrust.heading}
        spacing="lg"
      >
        <div className="grid items-start gap-10 lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)] lg:gap-12">
          <Stack gap={5}>
            <SectionHeader
              eyebrow={homeTrust.eyebrow}
              heading={homeTrust.heading}
              description={homeTrust.description}
            />
            <RegisteredIllustration
              id="ill-trust-rings"
              title="Trust as a system"
            />
          </Stack>
          <Stack gap={4}>
            {homeTrust.cards.map((card) => (
              <FeatureCard
                key={card.id}
                title={card.title}
                description={card.description}
                href={card.href}
                linkLabel={card.linkLabel}
                accent="slate"
              />
            ))}
          </Stack>
        </div>
      </SectionSurface>

      <CtaSection
        title={homeCta.title}
        description={homeCta.description}
        primaryCta={homeCta.primaryCta}
        background="dark"
        reveal={false}
      />
    </>
  );
}

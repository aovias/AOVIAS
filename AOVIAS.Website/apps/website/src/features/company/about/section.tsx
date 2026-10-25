import { BrandAtmosphere } from '@/components/brand';
import { ConnectedOrganizations } from '@/components/brand';
import { MarketingCtaLink } from '@/components/marketing/marketing-cta-link';
import {
  Container,
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { aboutContent, aboutMeta } from '@/features/company/about/content';
import { cn } from '@/lib/utils';

/**
 * Company hero — editorial institutional introduction.
 * ~45/55 split: left hierarchy, right architecture diagram.
 * Intentionally company-specific (not the shared marketing Hero).
 */
export function AboutSection() {
  return (
    <section
      id={aboutMeta.sectionId}
      aria-label={aboutMeta.title}
      className="border-border relative w-full overflow-x-clip border-b"
    >
      <BrandAtmosphere
        variant="corporate-gradient"
        quietRight
        density="rich"
      />

      <Container width="wide" className="relative py-hero-y lg:py-20 xl:py-24">
        <div
          className={cn(
            'grid min-w-0 items-center gap-10',
            'lg:grid-cols-[minmax(0,0.9fr)_minmax(0,1.1fr)] lg:gap-12 xl:gap-16 2xl:gap-x-20',
          )}
        >
          {/* Editorial column — eye path: heading → copy → CTA → trust */}
          <Stack gap={6} className="w-full min-w-0 max-w-xl xl:max-w-[32rem]">
            <Text
              as="p"
              size="eyebrow"
              className="text-domain-accent em-hero-compose font-semibold"
            >
              {aboutContent.eyebrow}
            </Text>

            <Heading
              level={1}
              size="hero-title"
              className="em-hero-compose max-w-[18ch] text-balance"
            >
              {aboutContent.titleLines[0]}
              <br />
              {aboutContent.titleLines[1]}
              <span className="text-domain-accent" aria-hidden="true">
                .
              </span>
            </Heading>

            <Stack gap={3} className="em-hero-compose max-w-md">
              <Paragraph size="body-lg" tone="muted" className="text-pretty">
                {aboutContent.lead}
              </Paragraph>

              <p className="text-domain-accent/90 text-small font-medium tracking-wide">
                {aboutContent.sectors.map((sector, index) => (
                  <span key={sector}>
                    {index > 0 ? (
                      <span
                        aria-hidden
                        className="text-muted-foreground/50 mx-2"
                      >
                        ·
                      </span>
                    ) : null}
                    {sector}
                  </span>
                ))}
              </p>

              <Paragraph size="body" tone="muted" className="text-pretty">
                {aboutContent.focus}
              </Paragraph>
            </Stack>

            <Stack
              direction="horizontal"
              gap={2}
              wrap
              className="em-hero-compose-cta pt-1"
            >
              <MarketingCtaLink
                {...aboutContent.primaryCta}
                variant="primary"
                size="lg"
                withArrow
              />
              <MarketingCtaLink
                {...aboutContent.secondaryCta}
                variant="soft"
                size="lg"
                withArrow
              />
            </Stack>

            {/* Minimal trust strip — typography only, no badges/icons */}
            <ul
              className="em-hero-compose text-muted-foreground flex list-none flex-wrap items-center gap-x-0 gap-y-2 p-0 pt-2"
              aria-label="Institutional credentials"
            >
              {aboutContent.trustSignals.map((signal, index) => (
                <li
                  key={signal}
                  className="text-caption flex items-center font-medium tracking-wide"
                >
                  {index > 0 ? (
                    <span
                      aria-hidden
                      className="text-domain-accent/55 mx-2.5 select-none"
                    >
                      ·
                    </span>
                  ) : null}
                  <span className="text-primary/80">{signal}</span>
                </li>
              ))}
            </ul>
          </Stack>

          {/* Architecture illustration — subordinate to headline */}
          <div
            className={cn(
              'em-hero-compose-illustration relative min-w-0',
              'md:min-h-[18rem] lg:min-h-[20rem] xl:min-h-[22rem]',
            )}
          >
            <div
              aria-hidden="true"
              className="bg-domain-accent/[0.07] absolute -inset-4 -z-10 hidden rounded-2xl blur-3xl lg:block"
            />
            <ConnectedOrganizations
              title={`${aboutMeta.title} — institutional platform architecture`}
            />
          </div>
        </div>
      </Container>
    </section>
  );
}

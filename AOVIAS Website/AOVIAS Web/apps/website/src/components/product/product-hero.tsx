import type { ReactNode } from 'react';

import {
  BrandAtmosphere,
  type EnterpriseAtmosphereVariant,
} from '@/components/brand';
import { MarketingCtaLink } from '@/components/marketing';
import { MediaSlotView, type MediaSlot } from '@/components/product/media-slot';
import type { FeatureHighlight, ProductCta } from '@/components/product/types';
import {
  Badge,
  Container,
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';
import type { AvlfAtmosphereId } from '@/registries/avlf';
import { resolveAtmosphereVariant } from '@/registries/avlf/atmosphere';

/**
 * ProductHero — data-driven product intro band.
 * Badge, name, description, CTAs, media slot, optional feature highlights.
 * Aligned with marketing Hero depth (atmosphere + large-screen composition).
 */
export type ProductHeroProps = {
  badge?: string;
  name: string;
  description?: string;
  primaryCta?: ProductCta;
  secondaryCta?: ProductCta;
  /** Primary visual — screenshot, diagram, demo, placeholder, etc. */
  media?: MediaSlot;
  /** Optional secondary media (e.g. illustration beside screenshot). */
  secondaryMedia?: MediaSlot;
  highlights?: readonly FeatureHighlight[];
  className?: string;
  /** Named EBAS asset (AVLS or live key). `true` preserves the legacy blueprint default. */
  atmosphere?: boolean | AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  /**
   * @deprecated Above-fold reveals hurt LCP — ignored. Kept for call-site compatibility.
   */
  reveal?: boolean;
  /** Extra slot below highlights (e.g. trust row). */
  footer?: ReactNode;
};

export function ProductHero({
  badge,
  name,
  description,
  primaryCta,
  secondaryCta,
  media,
  secondaryMedia,
  highlights,
  className,
  atmosphere = true,
  reveal: _reveal,
  footer,
}: ProductHeroProps) {
  void _reveal;
  const atmosphereVariant = resolveAtmosphereVariant(
    typeof atmosphere === 'string' ? atmosphere : true,
    'blueprint-grid',
  );

  return (
    <section
      className={cn(
        'border-border relative w-full overflow-x-clip border-b',
        atmosphere ? 'bg-transparent' : 'bg-surface-hero',
        className,
      )}
    >
      {atmosphere ? (
        <BrandAtmosphere
          variant={atmosphereVariant}
          quietRight
          density="rich"
        />
      ) : null}
      <Container
        width="wide"
        className="relative py-hero-y"
      >
        <div className="gap-grid-gap xl:gap-grid-gap-lg 2xl:gap-x-16 grid md:grid-cols-2 md:items-center xl:grid-cols-[minmax(0,1fr)_minmax(0,1.05fr)]">
          <Stack gap={6} className="max-w-xl xl:max-w-2xl 2xl:max-w-3xl">
            {badge ? <Badge variant="primary">{badge}</Badge> : null}
            <Heading level={1} size="hero-title">
              {name}
            </Heading>
            {description ? (
              <Paragraph size="body-lg" tone="muted">
                {description}
              </Paragraph>
            ) : null}
            {primaryCta || secondaryCta ? (
              <Stack direction="horizontal" gap={3} wrap>
                {primaryCta ? (
                  <MarketingCtaLink
                    {...primaryCta}
                    variant="primary"
                    size="lg"
                  />
                ) : null}
                {secondaryCta ? (
                  <MarketingCtaLink
                    {...secondaryCta}
                    variant="outline"
                    size="lg"
                  />
                ) : null}
              </Stack>
            ) : null}
            {highlights && highlights.length > 0 ? (
              <ul className="flex flex-col gap-2">
                {highlights.map((item) => (
                  <li key={item.id} className="flex items-start gap-2">
                    <span
                      aria-hidden="true"
                      className="bg-primary mt-2 size-1.5 shrink-0 rounded-full"
                    />
                    <Text as="span" size="small">
                      {item.label}
                    </Text>
                  </li>
                ))}
              </ul>
            ) : null}
            {footer}
          </Stack>

          <Stack
            gap={4}
            className="relative min-w-0 md:min-h-[17rem] lg:min-h-[19rem] xl:min-h-[21rem] 2xl:min-h-[24rem]"
          >
            <div
              aria-hidden="true"
              className="bg-domain-accent/10 absolute -inset-3 -z-10 hidden rounded-2xl blur-2xl md:block"
            />
            {media ? <MediaSlotView media={media} preset="hero" /> : null}
            {secondaryMedia ? (
              <MediaSlotView media={secondaryMedia} preset="content" />
            ) : null}
          </Stack>
        </div>
      </Container>
    </section>
  );
}

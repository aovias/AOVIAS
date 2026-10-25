import type { ReactNode } from 'react';
import type { LucideIcon } from 'lucide-react';

import {
  BrandAtmosphere,
  type EnterpriseAtmosphereVariant,
} from '@/components/brand';
import { MarketingCtaLink } from '@/components/marketing/marketing-cta-link';
import type {
  MarketingAlign,
  MarketingCta,
  MarketingHighlight,
} from '@/components/marketing/types';
import {
  Container,
  Heading,
  Paragraph,
  Stack,
  Text,
  type HeadingSize,
} from '@/components/primitives';
import { cn } from '@/lib/utils';
import type { AvlfAtmosphereId } from '@/registries/avlf';
import { resolveAtmosphereVariant } from '@/registries/avlf/atmosphere';

/**
 * Hero — reusable page-intro band. All content via props; no page assumptions.
 * V11: no Framer reveal import (LCP-critical shared component).
 */
export type HeroActionLink = MarketingCta & {
  id?: string;
  icon?: LucideIcon;
};

export type HeroProps = {
  eyebrow?: string;
  title: string;
  description?: string;
  primaryCta?: MarketingCta;
  secondaryCta?: MarketingCta & { icon?: LucideIcon };
  tertiaryCta?: MarketingCta & { icon?: LucideIcon };
  /** Optional trust strip (icon+label) or capability grid (with description). */
  highlights?: readonly MarketingHighlight[];
  /** Text links under CTAs (e.g. Book Consultation). */
  actionLinks?: readonly HeroActionLink[];
  /** Trailing arrows on hero CTAs. */
  ctaWithArrow?: boolean;
  illustration?: ReactNode;
  /** Tokenized background utilities, e.g. `bg-muted/40`. */
  backgroundClassName?: string;
  align?: MarketingAlign;
  className?: string;
  /** @deprecated No-op — retained for call-site compatibility. */
  reveal?: boolean;
  /** Heading size token — default hero-title (same scale as page-title). */
  titleSize?: HeadingSize;
  /** Named EBAS asset (AVLS or live key). `true` preserves the legacy blueprint default. */
  atmosphere?: boolean | AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  /** Quieter right-side glow when an illustration occupies that column. */
  atmosphereQuietRight?: boolean;
};

export function Hero({
  eyebrow,
  title,
  description,
  primaryCta,
  secondaryCta,
  tertiaryCta,
  highlights,
  actionLinks,
  ctaWithArrow = false,
  illustration,
  backgroundClassName,
  align = 'start',
  className,
  reveal: _reveal = false,
  titleSize = 'hero-title',
  atmosphere = false,
  atmosphereQuietRight = true,
}: HeroProps) {
  void _reveal;
  const isCenter = align === 'center';
  const hasIllustration = Boolean(illustration);
  const atmosphereVariant = resolveAtmosphereVariant(
    typeof atmosphere === 'string' ? atmosphere : true,
    'blueprint-grid',
  );
  const capabilityGrid =
    Boolean(highlights?.length) &&
    highlights!.some((item) => Boolean(item.description));
  const hasCtas = Boolean(primaryCta || secondaryCta || tertiaryCta);

  return (
    <section
      className={cn(
        'border-border relative w-full overflow-x-clip border-b',
        backgroundClassName ??
          (atmosphere ? 'bg-transparent' : 'bg-surface-hero'),
        className,
      )}
    >
      {atmosphere ? (
        <BrandAtmosphere
          variant={atmosphereVariant}
          quietRight={atmosphereQuietRight && hasIllustration}
          density="rich"
        />
      ) : null}
      <Container
        width="wide"
        className="relative py-hero-y"
      >
        <div
          className={cn(
            'gap-grid-gap xl:gap-grid-gap-lg grid min-w-0',
            hasIllustration &&
              'md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)] lg:grid-cols-2 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.05fr)] 2xl:gap-x-16',
            hasIllustration &&
              (capabilityGrid ? 'md:items-start' : 'md:items-center'),
            isCenter && !hasIllustration && 'justify-items-center',
          )}
        >
          <Stack
            gap={capabilityGrid ? 4 : 6}
            className={cn(
              'w-full min-w-0 max-w-2xl xl:max-w-3xl',
              capabilityGrid && 'pt-1',
              isCenter && !hasIllustration && 'items-center text-center',
            )}
          >
            {eyebrow ? (
              <Text
                as="p"
                size="eyebrow"
                className="text-domain-accent em-hero-compose font-semibold"
              >
                {eyebrow}
              </Text>
            ) : null}
            <Heading level={1} size={titleSize}>
              {title}
            </Heading>
            {description ? (
              <Paragraph
                size="body-lg"
                tone="muted"
                className={cn(
                  'em-hero-compose',
                  isCenter && !hasIllustration && 'text-center',
                )}
              >
                {description}
              </Paragraph>
            ) : null}

            {capabilityGrid && highlights ? (
              <ul
                className={cn(
                  'em-hero-compose grid w-full grid-cols-1 gap-x-5 gap-y-1 sm:grid-cols-2',
                  isCenter && !hasIllustration && 'justify-items-center',
                )}
              >
                {highlights.map((item) => {
                  const Icon = item.icon;
                  return (
                    <li
                      key={item.id}
                      className="flex min-w-0 items-start gap-2.5 py-2 sm:py-2.5"
                    >
                      <span className="bg-primary/10 text-domain-accent mt-0.5 flex size-8 shrink-0 items-center justify-center rounded-md">
                        <Icon
                          className="size-3.5 stroke-[1.5]"
                          aria-hidden="true"
                        />
                      </span>
                      <span className="min-w-0">
                        <span className="text-primary text-caption block font-semibold leading-snug sm:text-small">
                          {item.label}
                        </span>
                        {item.description ? (
                          <span className="text-muted-foreground text-caption mt-0.5 block leading-snug opacity-90">
                            {item.description}
                          </span>
                        ) : null}
                      </span>
                    </li>
                  );
                })}
              </ul>
            ) : null}

            {hasCtas ? (
              <Stack
                direction="horizontal"
                gap={2}
                wrap
                className={cn(
                  'em-hero-compose-cta w-full',
                  isCenter && !hasIllustration && 'justify-center',
                )}
              >
                {primaryCta ? (
                  <MarketingCtaLink
                    {...primaryCta}
                    variant="primary"
                    size={capabilityGrid ? 'md' : 'lg'}
                    withArrow={ctaWithArrow}
                    className="w-full sm:w-auto"
                  />
                ) : null}
                {secondaryCta ? (
                  <MarketingCtaLink
                    label={secondaryCta.label}
                    href={secondaryCta.href}
                    {...(secondaryCta.external
                      ? { external: secondaryCta.external }
                      : {})}
                    {...(secondaryCta.icon ? { icon: secondaryCta.icon } : {})}
                    variant="soft"
                    size={capabilityGrid ? 'md' : 'lg'}
                    withArrow={ctaWithArrow && !secondaryCta.icon}
                    className="w-full sm:w-auto"
                  />
                ) : null}
                {tertiaryCta ? (
                  <MarketingCtaLink
                    label={tertiaryCta.label}
                    href={tertiaryCta.href}
                    {...(tertiaryCta.external
                      ? { external: tertiaryCta.external }
                      : {})}
                    {...(tertiaryCta.icon ? { icon: tertiaryCta.icon } : {})}
                    variant="soft"
                    size={capabilityGrid ? 'md' : 'lg'}
                    className="w-full sm:w-auto"
                  />
                ) : null}
              </Stack>
            ) : null}

            {actionLinks && actionLinks.length > 0 ? (
              <Stack
                direction="horizontal"
                gap={4}
                wrap
                className={cn(
                  'em-hero-compose -mt-1',
                  isCenter && !hasIllustration && 'justify-center',
                )}
              >
                {actionLinks.map((link) => (
                  <MarketingCtaLink
                    key={link.id ?? link.href + link.label}
                    label={link.label}
                    href={link.href}
                    {...(link.external ? { external: link.external } : {})}
                    {...(link.icon ? { icon: link.icon } : {})}
                    variant="link"
                    size="sm"
                    withArrow
                    className="text-primary"
                  />
                ))}
              </Stack>
            ) : null}

            {!capabilityGrid && highlights && highlights.length > 0 ? (
              <ul
                className={cn(
                  'em-hero-compose text-primary flex flex-wrap items-stretch',
                  isCenter && !hasIllustration && 'justify-center',
                )}
              >
                {highlights.map((item, index) => {
                  const Icon = item.icon;
                  return (
                    <li
                      key={item.id}
                      className={cn(
                        'flex min-w-[7.5rem] flex-col items-center gap-2 px-4 py-1 text-center sm:min-w-0 sm:flex-1',
                        index > 0 && 'border-border/50 border-l',
                      )}
                    >
                      <Icon
                        className="size-5 shrink-0 stroke-[1.5]"
                        aria-hidden="true"
                      />
                      <span className="text-caption font-medium leading-snug">
                        {item.label}
                      </span>
                    </li>
                  );
                })}
              </ul>
            ) : null}
          </Stack>

          {illustration ? (
            <div
              className={cn(
                'em-hero-compose-illustration relative min-w-0',
                'max-md:hidden',
                'md:min-h-[17rem] lg:min-h-[19rem] xl:min-h-[21rem] 2xl:min-h-[24rem]',
                'xl:translate-x-1 2xl:translate-x-2',
              )}
            >
              <div
                aria-hidden="true"
                className="bg-domain-accent/10 absolute -inset-3 -z-10 hidden rounded-2xl blur-2xl md:block"
              />
              {illustration}
            </div>
          ) : null}
        </div>
      </Container>
    </section>
  );
}

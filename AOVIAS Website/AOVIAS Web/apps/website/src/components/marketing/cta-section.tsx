import { BrandAtmosphere } from '@/components/brand';
import { MarketingCtaLink } from '@/components/marketing/marketing-cta-link';
import type { MarketingCta } from '@/components/marketing/types';
import {
  Container,
  Heading,
  Paragraph,
  Stack,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * CtaSection — mid/end-page call-to-action band with enterprise surface depth.
 * `dark` renders the institutional navy band that closes major pages.
 * V11: no Framer reveal wrapper (keeps motion off the shared CTA import path).
 */
export type CtaSectionBackground =
  | 'muted'
  | 'subtle'
  | 'inverse'
  | 'none'
  | 'tint'
  | 'dark';

export type CtaSectionProps = {
  title: string;
  description?: string;
  primaryCta?: MarketingCta;
  secondaryCta?: MarketingCta;
  background?: CtaSectionBackground;
  className?: string;
  /** @deprecated No-op — retained for call-site compatibility. */
  reveal?: boolean;
};

const bandClass: Record<CtaSectionBackground, string> = {
  muted: 'bg-surface-muted border-border border',
  subtle: 'bg-surface-subtle border-border border',
  tint: 'bg-primary-muted border-border border',
  inverse: 'bg-primary text-primary-foreground border-transparent',
  dark: 'border-brand-800/60 bg-gradient-to-br from-brand-950 via-brand-900 to-brand-950 text-brand-50 border',
  none: 'border-transparent bg-transparent',
};

export function CtaSection({
  title,
  description,
  primaryCta,
  secondaryCta,
  background = 'dark',
  className,
  reveal: _reveal = true,
}: CtaSectionProps) {
  void _reveal;
  const isInverse = background === 'inverse';
  const isDark = background === 'dark';
  const onDarkSurface = isInverse || isDark;

  const inner = (
    <div className="relative overflow-hidden rounded-xl px-6 py-stack-xl md:px-10 lg:px-14">
      {background !== 'none' && !onDarkSurface ? (
        <BrandAtmosphere
          variant="subtle-texture"
          density="subtle"
          className="opacity-70"
        />
      ) : null}
      {isDark ? (
        <>
          {/* Institutional blueprint texture + brand glow for the dark band. */}
          <div
            aria-hidden="true"
            className="pointer-events-none absolute inset-0 opacity-[0.06]"
            style={{
              backgroundImage:
                'linear-gradient(to right, color-mix(in srgb, var(--brand-50) 70%, transparent) 1px, transparent 1px), linear-gradient(to bottom, color-mix(in srgb, var(--brand-50) 70%, transparent) 1px, transparent 1px)',
              backgroundSize: '44px 44px',
              maskImage:
                'radial-gradient(110% 90% at 50% 0%, black, transparent 75%)',
            }}
          />
          <div
            aria-hidden="true"
            className="bg-primary/25 pointer-events-none absolute -top-20 left-1/2 h-52 w-[36rem] -translate-x-1/2 rounded-full blur-3xl"
          />
          <div
            aria-hidden="true"
            className="via-primary/50 absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent to-transparent"
          />
        </>
      ) : null}
      <Stack
        gap={5}
        align="center"
        className="relative mx-auto max-w-2xl text-center"
      >
        <Heading
          level={2}
          size="section-title"
          className={cn(
            isInverse && 'text-primary-foreground',
            isDark && 'text-brand-50',
          )}
        >
          {title}
        </Heading>
        {description ? (
          <Paragraph
            tone={onDarkSurface ? 'default' : 'muted'}
            className={cn(
              'text-center',
              isInverse && 'text-primary-foreground/85',
              isDark && 'text-brand-200/90',
            )}
          >
            {description}
          </Paragraph>
        ) : null}
        {primaryCta || secondaryCta ? (
          <Stack direction="horizontal" gap={3} wrap className="justify-center">
            {primaryCta ? (
              <MarketingCtaLink
                {...primaryCta}
                variant={isInverse ? 'secondary' : 'primary'}
                size="lg"
                {...(isDark
                  ? {
                      className:
                        'bg-brand-50 text-brand-950 hover:bg-brand-100 border-transparent shadow-raised',
                    }
                  : {})}
              />
            ) : null}
            {secondaryCta ? (
              <MarketingCtaLink
                {...secondaryCta}
                variant="outline"
                size="lg"
                {...(isInverse
                  ? {
                      className:
                        'border-background/40 bg-transparent text-background hover:bg-background/10',
                    }
                  : {})}
                {...(isDark
                  ? {
                      className:
                        'border-brand-400/40 bg-transparent text-brand-50 hover:bg-brand-800/50 hover:border-brand-300/50',
                    }
                  : {})}
              />
            ) : null}
          </Stack>
        ) : null}
      </Stack>
    </div>
  );

  return (
    <section className={cn('w-full py-section-y-lg', className)}>
      <Container width="wide">
        <div className={cn('shadow-raised rounded-xl', bandClass[background])}>
          {inner}
        </div>
      </Container>
    </section>
  );
}

import { MarketingCtaLink } from '@/components/marketing';
import { MarketingReveal } from '@/components/marketing/reveal';
import type { EnterpriseOffering } from '@/components/pricing/models';
import {
  Container,
  Heading,
  Paragraph,
  Stack,
  Surface,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * EnterpriseCta — enterprise messaging with contact / demo / consultation CTAs.
 */
export type EnterpriseCtaProps = {
  offering: EnterpriseOffering;
  className?: string;
  reveal?: boolean;
  background?: 'muted' | 'subtle' | 'none';
};

export function EnterpriseCta({
  offering,
  className,
  reveal = true,
  background = 'muted',
}: EnterpriseCtaProps) {
  const inner = (
    <Stack gap={5} className="mx-auto max-w-2xl text-center" align="center">
      <Heading level={2} size="h2">
        {offering.title}
      </Heading>
      {offering.description ? (
        <Paragraph tone="muted" className="text-center">
          {offering.description}
        </Paragraph>
      ) : null}
      <Stack direction="horizontal" gap={3} wrap className="justify-center">
        <MarketingCtaLink {...offering.primaryCta} variant="primary" size="lg" />
        {offering.secondaryCta ? (
          <MarketingCtaLink
            {...offering.secondaryCta}
            variant="outline"
            size="lg"
          />
        ) : null}
        {offering.tertiaryCta ? (
          <MarketingCtaLink
            {...offering.tertiaryCta}
            variant="ghost"
            size="lg"
          />
        ) : null}
      </Stack>
    </Stack>
  );

  const content = reveal ? (
    <MarketingReveal enabled={reveal}>{inner}</MarketingReveal>
  ) : (
    inner
  );

  if (background === 'none') {
    return (
      <section className={cn('w-full py-section-y-lg', className)}>
        <Container width="wide">{content}</Container>
      </section>
    );
  }

  return (
    <section className={cn('w-full py-section-y-lg', className)}>
      <Container width="wide">
        <Surface
          tone={background}
          padding="none"
          className="rounded-lg px-6 py-12 md:px-10"
        >
          {content}
        </Surface>
      </Container>
    </section>
  );
}

import type { ReactNode } from 'react';

import { MarketingReveal } from '@/components/marketing/reveal';
import { MediaSlotView, type MediaSlot } from '@/components/product/media-slot';
import {
  Container,
  Heading,
  IconWrapper,
  Paragraph,
  Stack,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * FeatureShowcase — single feature row with media on left or right.
 */
export type FeatureShowcaseProps = {
  title: string;
  description: string;
  icon?: ReactNode;
  media?: MediaSlot;
  /** Media on the start (left in LTR) or end side. */
  align?: 'start' | 'end';
  className?: string;
  reveal?: boolean;
  id?: string;
};

export function FeatureShowcase({
  title,
  description,
  icon,
  media,
  align = 'end',
  className,
  reveal = true,
  id,
}: FeatureShowcaseProps) {
  const copy = (
    <Stack gap={4} className="max-w-lg">
      {icon ? (
        <IconWrapper tone="domain" size="lg">
          {icon}
        </IconWrapper>
      ) : null}
      <Heading level={3} size="h3">
        {title}
      </Heading>
      <Paragraph tone="muted">{description}</Paragraph>
    </Stack>
  );

  const mediaNode = media ? <MediaSlotView media={media} /> : null;

  return (
    <section id={id} className={cn('w-full py-section-y-md', className)}>
      <Container width="wide">
        <MarketingReveal enabled={reveal}>
          <div className="gap-grid-gap grid lg:grid-cols-2 lg:items-center">
            {align === 'start' ? (
              <>
                {mediaNode}
                {copy}
              </>
            ) : (
              <>
                {copy}
                {mediaNode}
              </>
            )}
          </div>
        </MarketingReveal>
      </Container>
    </section>
  );
}

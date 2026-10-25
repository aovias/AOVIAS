import { MarketingReveal } from '@/components/marketing/reveal';
import { MediaSlotView, type MediaSlot } from '@/components/product/media-slot';
import {
  Container,
  Grid,
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

export type ProductOverviewMetric = {
  id: string;
  value: string;
  label: string;
};

/**
 * ProductOverview — narrative overview with optional media and metrics.
 */
export type ProductOverviewProps = {
  heading: string;
  description: string;
  media?: MediaSlot;
  metrics?: readonly ProductOverviewMetric[];
  className?: string;
  reveal?: boolean;
  /** Media on start (default) or end. */
  mediaPosition?: 'start' | 'end';
};

export function ProductOverview({
  heading,
  description,
  media,
  metrics,
  className,
  reveal = true,
  mediaPosition = 'end',
}: ProductOverviewProps) {
  const copy = (
    <Stack gap={4} className="max-w-xl">
      <Heading level={2} size="h2">
        {heading}
      </Heading>
      <Paragraph tone="muted">{description}</Paragraph>
      {metrics && metrics.length > 0 ? (
        <Grid cols={metrics.length >= 3 ? 3 : 2} gap={4} className="mt-2">
          {metrics.map((metric) => (
            <Stack key={metric.id} gap={1}>
              <Text as="p" className="text-h3 font-semibold tracking-tight">
                {metric.value}
              </Text>
              <Text as="p" size="small" tone="muted">
                {metric.label}
              </Text>
            </Stack>
          ))}
        </Grid>
      ) : null}
    </Stack>
  );

  const mediaNode = media ? <MediaSlotView media={media} /> : null;

  return (
    <section className={cn('w-full py-section-y-lg', className)}>
      <Container width="wide">
        <MarketingReveal enabled={reveal}>
          <div
            className={cn(
              'grid gap-10 lg:items-center',
              media ? 'lg:grid-cols-2' : '',
            )}
          >
            {mediaPosition === 'start' && mediaNode}
            {copy}
            {mediaPosition === 'end' && mediaNode}
          </div>
        </MarketingReveal>
      </Container>
    </section>
  );
}

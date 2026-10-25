import { MarketingCtaLink } from '@/components/marketing';
import { MediaSlotView, type MediaSlot } from '@/components/product/media-slot';
import type { ProductCta } from '@/components/product/types';
import {
  Caption,
  Container,
  Heading,
  Paragraph,
  Stack,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * ArchitectureDiagramBlock — generic diagram presentation (not product-tied).
 * Diagram content is always a MediaSlot (diagram, image, custom, placeholder).
 */
export type ArchitectureDiagramBlockProps = {
  title: string;
  description?: string;
  diagram: MediaSlot;
  caption?: string;
  download?: ProductCta;
  className?: string;
  id?: string;
};

export function ArchitectureDiagramBlock({
  title,
  description,
  diagram,
  caption,
  download,
  className,
  id,
}: ArchitectureDiagramBlockProps) {
  return (
    <section id={id} className={cn('w-full py-section-y-lg', className)}>
      <Container width="wide">
        <Stack gap={6}>
          <Stack gap={3} className="max-w-2xl">
            <Heading level={2} size="h2">
              {title}
            </Heading>
            {description ? <Paragraph tone="muted">{description}</Paragraph> : null}
          </Stack>
          <figure className="min-w-0">
            <MediaSlotView media={diagram} preset="hero" />
            {caption ? (
              <figcaption className="mt-3">
                <Caption>{caption}</Caption>
              </figcaption>
            ) : null}
          </figure>
          {download ? (
            <div>
              <MarketingCtaLink {...download} variant="outline" size="sm" />
            </div>
          ) : null}
        </Stack>
      </Container>
    </section>
  );
}

import type { ReactNode } from 'react';

import {
  AvatarPlaceholder,
  Caption,
  Quote,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * QuoteBlock — testimonial / leadership quote with attribution.
 * Composes the Quote primitive; does not invent page copy.
 */
export type QuoteBlockProps = {
  quote: string;
  author: string;
  position?: string;
  organization?: string;
  avatar?: ReactNode;
  avatarInitials?: string;
  className?: string;
};

export function QuoteBlock({
  quote,
  author,
  position,
  organization,
  avatar,
  avatarInitials,
  className,
}: QuoteBlockProps) {
  const attribution = [position, organization].filter(Boolean).join(', ');

  return (
    <figure
      className={cn(
        'border-domain-accent/50 relative max-w-3xl border-l-2 pl-6',
        className,
      )}
    >
      <span
        aria-hidden="true"
        className="text-domain-accent/30 pointer-events-none absolute -top-4 left-3 font-serif text-6xl leading-none select-none"
      >
        “
      </span>
      <Quote className="text-lead not-italic">
        <span className="sr-only">Quote:</span>
        {quote}
      </Quote>
      <figcaption className="mt-6">
        <Stack direction="horizontal" gap={3} align="center">
          {avatar ?? (
            <AvatarPlaceholder
              initials={avatarInitials ?? author}
              label={author}
              size="md"
            />
          )}
          <Stack gap={1}>
            <Text as="p" weight="medium" size="small">
              {author}
            </Text>
            {attribution ? <Caption>{attribution}</Caption> : null}
          </Stack>
        </Stack>
      </figcaption>
    </figure>
  );
}

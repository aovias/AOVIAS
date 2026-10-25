import {
  Caption,
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * Timeline — vertical or horizontal event sequence. Events via props only.
 */
export type TimelineEvent = {
  id: string;
  date?: string;
  title: string;
  description?: string;
};

export type TimelineProps = {
  events: readonly TimelineEvent[];
  orientation?: 'vertical' | 'horizontal';
  className?: string;
};

export function Timeline({
  events,
  orientation = 'vertical',
  className,
}: TimelineProps) {
  if (orientation === 'horizontal') {
    return (
      <ol
        className={cn(
          'flex snap-x gap-6 overflow-x-auto pb-4 md:grid md:snap-none md:overflow-visible',
          events.length <= 4 ? 'md:grid-cols-4' : 'md:grid-cols-none md:flex',
          className,
        )}
      >
        {events.map((event, index) => (
          <li
            key={event.id}
            className="border-border relative min-w-[14rem] flex-1 snap-start border-t-2 pt-4 md:min-w-0"
          >
            <Stack gap={2}>
              {event.date ? (
                <Caption className="font-medium">{event.date}</Caption>
              ) : (
                <Caption className="font-medium">Step {index + 1}</Caption>
              )}
              <Heading level={3} size="h5">
                {event.title}
              </Heading>
              {event.description ? (
                <Paragraph size="small" tone="muted">
                  {event.description}
                </Paragraph>
              ) : null}
            </Stack>
          </li>
        ))}
      </ol>
    );
  }

  return (
    <ol className={cn('relative ms-3 border-s border-border', className)}>
      {events.map((event) => (
        <li key={event.id} className="relative ms-6 pb-10 last:pb-0">
          <span
            aria-hidden="true"
            className="bg-primary absolute top-1.5 -start-[1.9rem] size-3 rounded-full ring-4 ring-background"
          />
          <Stack gap={2}>
            {event.date ? (
              <Text as="p" size="caption" tone="muted" weight="medium">
                <time dateTime={event.date}>{event.date}</time>
              </Text>
            ) : null}
            <Heading level={3} size="h5">
              {event.title}
            </Heading>
            {event.description ? (
              <Paragraph size="small" tone="muted">
                {event.description}
              </Paragraph>
            ) : null}
          </Stack>
        </li>
      ))}
    </ol>
  );
}

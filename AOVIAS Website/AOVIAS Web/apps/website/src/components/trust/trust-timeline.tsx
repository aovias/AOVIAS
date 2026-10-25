import { Timeline } from '@/components/marketing';
import type { TrustTimelineEvent } from '@/components/trust/models';
import { cn } from '@/lib/utils';

/**
 * TrustTimeline — institutional milestones from TrustTimelineEvent models.
 * Composes the marketing Timeline primitive.
 */
export type TrustTimelineProps = {
  events: readonly TrustTimelineEvent[];
  orientation?: 'vertical' | 'horizontal';
  className?: string;
};

export function TrustTimeline({
  events,
  orientation = 'vertical',
  className,
}: TrustTimelineProps) {
  return (
    <Timeline
      orientation={orientation}
      className={cn(className)}
      events={events.map((event) => ({
        id: event.id,
        title: event.title,
        ...(event.date ? { date: event.date } : {}),
        ...(event.description ? { description: event.description } : {}),
      }))}
    />
  );
}

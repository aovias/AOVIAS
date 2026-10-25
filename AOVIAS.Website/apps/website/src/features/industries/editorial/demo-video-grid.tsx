import {
  HealthcareVideoCard,
  type HealthcareVideoCardProps,
} from '@/features/industries/editorial/healthcare-video-card';
import { cn } from '@/lib/utils';

export type DemoVideoItem = Omit<HealthcareVideoCardProps, 'className'> & {
  id: string;
};

export type DemoVideoGridProps = {
  videos: readonly DemoVideoItem[];
  className?: string;
};

/**
 * Responsive grid of YouTube demo cards — append items to grow the grid.
 */
export function DemoVideoGrid({ videos, className }: DemoVideoGridProps) {
  if (videos.length === 0) return null;

  return (
    <ul
      className={cn(
        'm-0 grid list-none gap-10 p-0',
        'grid-cols-1 md:grid-cols-2',
        className,
      )}
    >
      {videos.map((video) => (
        <li key={video.id} className="min-w-0">
          <HealthcareVideoCard
            title={video.title}
            description={video.description}
            youtubeId={video.youtubeId}
            {...(video.duration ? { duration: video.duration } : {})}
            {...(video.theme ? { theme: video.theme } : {})}
            {...(video.badge ? { badge: video.badge } : {})}
          />
        </li>
      ))}
    </ul>
  );
}

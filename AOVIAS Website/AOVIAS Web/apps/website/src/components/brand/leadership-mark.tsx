import { cn } from '@/lib/utils';

export type LeadershipMarkProps = {
  name: string;
  initials?: string;
  className?: string;
};

/**
 * Non-photo institutional avatar mark until leadership photography exists.
 */
export function LeadershipMark({ name, initials, className }: LeadershipMarkProps) {
  const mark =
    initials ??
    name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');

  return (
    <div
      role="img"
      aria-label={`${name} portrait placeholder`}
      className={cn(
        'border-border bg-muted/50 text-primary relative flex aspect-square w-full items-center justify-center overflow-hidden rounded-lg border',
        className,
      )}
    >
      <svg viewBox="0 0 120 120" className="absolute inset-0 size-full text-primary" aria-hidden>
        <circle cx="60" cy="60" r="48" fill="currentColor" opacity="0.06" />
        <circle cx="60" cy="48" r="16" fill="currentColor" opacity="0.14" />
        <path
          d="M28 96 C36 76 48 68 60 68 C72 68 84 76 92 96"
          fill="currentColor"
          opacity="0.12"
        />
      </svg>
      <span className="relative z-10 text-lg font-semibold tracking-wide">{mark}</span>
    </div>
  );
}

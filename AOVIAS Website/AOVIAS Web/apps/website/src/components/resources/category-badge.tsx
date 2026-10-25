import type { ContentCategory } from '@/components/content';
import { Badge, IconWrapper } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * CategoryBadge — content category chip with optional icon and active state.
 */
export type CategoryBadgeProps = {
  category: ContentCategory;
  active?: boolean;
  className?: string;
  /** Render as button for filter chips. */
  onClick?: () => void;
};

const toneToBadge = {
  default: 'default',
  primary: 'primary',
  secondary: 'secondary',
  info: 'info',
  success: 'success',
  warning: 'warning',
} as const;

export function CategoryBadge({
  category,
  active = false,
  className,
  onClick,
}: CategoryBadgeProps) {
  const tone = category.tone ?? 'default';
  const content = (
    <>
      {category.icon ? (
        <IconWrapper
          tone="muted"
          size="sm"
          decorative
          className="me-1 inline-flex"
        >
          {category.icon}
        </IconWrapper>
      ) : null}
      {category.name}
    </>
  );

  const classes = cn(
    'inline-flex items-center',
    active && 'ring-ring ring-2 ring-offset-2 ring-offset-background',
    className,
  );

  if (onClick) {
    return (
      <button
        type="button"
        onClick={onClick}
        aria-pressed={active}
        className={cn('focus-visible:ring-ring rounded-md focus-visible:ring-2 focus-visible:outline-none')}
      >
        <Badge variant={toneToBadge[tone]} className={classes}>
          {content}
        </Badge>
      </button>
    );
  }

  return (
    <Badge
      variant={toneToBadge[tone]}
      className={classes}
      {...(active ? { 'aria-current': 'true' as const } : {})}
    >
      {content}
    </Badge>
  );
}

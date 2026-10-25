import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/**
 * AvatarPlaceholder — generic avatar slot (initials or icon). No user domain logic.
 */
export type AvatarSize = 'sm' | 'md' | 'lg' | 'xl';

export type AvatarPlaceholderProps = {
  initials?: string;
  children?: ReactNode;
  size?: AvatarSize;
  className?: string;
  label?: string;
};

const sizeClass: Record<AvatarSize, string> = {
  sm: 'size-8 text-caption',
  md: 'size-10 text-small',
  lg: 'size-12 text-body',
  xl: 'size-16 text-h5',
};

export function AvatarPlaceholder({
  initials,
  children,
  size = 'md',
  className,
  label = 'Avatar',
}: AvatarPlaceholderProps) {
  return (
    <span
      role="img"
      aria-label={label}
      className={cn(
        'bg-muted text-muted-foreground inline-flex items-center justify-center rounded-full font-medium select-none',
        sizeClass[size],
        className,
      )}
    >
      {children ?? (initials ? initials.slice(0, 2).toUpperCase() : '?')}
    </span>
  );
}

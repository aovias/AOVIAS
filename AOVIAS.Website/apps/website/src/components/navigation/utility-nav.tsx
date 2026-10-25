import Link from 'next/link';

import { getVisibleUtilityItems, type UtilityItem } from '@/config/navigation';
import { cn } from '@/lib/utils';

export type UtilityNavProps = {
  items?: readonly UtilityItem[];
  className?: string;
  /** Dense inline treatment for header chrome. */
  density?: 'header' | 'footer';
};

export function UtilityNav({
  items,
  className,
  density = 'header',
}: UtilityNavProps) {
  const visible = getVisibleUtilityItems(items);

  if (visible.length === 0) return null;

  return (
    <nav
      aria-label="Utility"
      className={cn(
        density === 'header' && 'hidden items-center gap-3 md:flex',
        density === 'footer' && 'flex flex-wrap items-center gap-3',
        className,
      )}
    >
      {visible.map((item) => (
        <Link
          key={item.id}
          href={item.href}
          target={item.external ? '_blank' : undefined}
          rel={item.external ? 'noreferrer' : undefined}
          className={cn(
            'focus-visible:ring-ring text-small font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none',
            density === 'header'
              ? 'text-muted-foreground hover:text-foreground'
              : 'text-muted-foreground hover:text-foreground',
          )}
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}

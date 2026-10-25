import Link from 'next/link';
import { CircleUserRound, Settings } from 'lucide-react';

import { cn } from '@/lib/utils';

export type HeaderIconActionsProps = {
  className?: string;
};

const iconActionClass =
  'text-nav-foreground hover:text-nav-foreground-hover focus-visible:ring-focus-ring inline-flex size-6 shrink-0 items-center justify-center rounded-md border border-border bg-muted transition-colors focus-visible:ring-2 focus-visible:outline-none';

/**
 * Compact header account actions — matches ThemeToggle density (size-6 = 32px).
 */
export function HeaderIconActions({ className }: HeaderIconActionsProps) {
  return (
    <div
      className={cn('hidden items-center gap-1.5 sm:flex', className)}
      aria-label="Account"
    >
      <Link
        href="/auth/sign-in"
        className={iconActionClass}
        aria-label="Sign in"
        title="Sign in"
      >
        <CircleUserRound className="size-3.5" aria-hidden="true" />
      </Link>
      <Link
        href="/company/contact"
        className={iconActionClass}
        aria-label="Settings and support"
        title="Settings and support"
      >
        <Settings className="size-3.5" aria-hidden="true" />
      </Link>
    </div>
  );
}

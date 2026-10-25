import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type PageHeaderProps = {
  title: string;
  description?: string;
  eyebrow?: string;
  actions?: ReactNode;
  className?: string;
};

export function PageHeader({ title, description, eyebrow, actions, className }: PageHeaderProps) {
  return (
    <header className={cn('mb-8 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between', className)}>
      <div className="min-w-0">
        {eyebrow ? (
          <p className="text-domain-accent text-eyebrow mb-stack-sm font-medium tracking-wide uppercase">
            {eyebrow}
          </p>
        ) : null}
        <h1 className="text-page-title text-ink font-semibold tracking-tight">
          {title}
        </h1>
        {description ? (
          <p className="text-ink-muted text-lead mt-3 max-w-prose">{description}</p>
        ) : null}
      </div>
      {actions ? <div className="shrink-0">{actions}</div> : null}
    </header>
  );
}

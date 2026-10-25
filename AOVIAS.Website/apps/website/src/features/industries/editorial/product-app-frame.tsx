import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

export type ProductAppFrameProps = {
  children: ReactNode;
  className?: string;
  /** Accessible name for the framed product surface. */
  label: string;
  variant?: 'desktop' | 'mobile';
};

/**
 * Premium application chrome for product screenshots — dark glass border,
 * soft elevation, no browser / OS chrome.
 */
export function ProductAppFrame({
  children,
  className,
  label,
  variant = 'desktop',
}: ProductAppFrameProps) {
  if (variant === 'mobile') {
    return (
      <div
        className={cn(
          'group/frame mx-auto w-1/2 max-w-[9rem] pt-8 sm:pt-10',
          className,
        )}
      >        <div
          className={cn(
            'border-border/70 bg-[#0c121e] relative overflow-hidden rounded-[1.75rem] border',
            'shadow-[0_24px_48px_-12px_rgba(2,8,20,0.65),0_0_0_1px_rgba(90,143,208,0.12)]',
            'transition-[transform,box-shadow] duration-500 ease-out',
            'motion-safe:group-hover/frame:translate-y-[-2px]',
            'motion-safe:group-hover/frame:shadow-[0_28px_56px_-12px_rgba(2,8,20,0.72),0_0_0_1px_rgba(90,143,208,0.18)]',
          )}
        >
          <div className="bg-[#121a28] flex justify-center px-6 pt-3 pb-2">
            <span className="bg-[#1a2538] h-1.5 w-16 rounded-full" aria-hidden />
          </div>
          <div className="relative aspect-[9/16] overflow-hidden bg-[#0a1018]">
            <div className="size-full transition-transform duration-700 ease-out motion-safe:group-hover/frame:scale-[1.025]">
              {children}
            </div>
          </div>
          <div className="bg-[#121a28] flex justify-center py-3">
            <span className="border-border/60 size-8 rounded-full border" aria-hidden />
          </div>
          <span className="sr-only">{label}</span>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('group/frame w-full', className)}>
      <div
        className={cn(
          'border-border/70 bg-[#0c121e] relative overflow-hidden rounded-2xl border',
          'shadow-[0_28px_56px_-16px_rgba(2,8,20,0.7),0_0_0_1px_rgba(90,143,208,0.14)]',
          'ring-1 ring-inset ring-white/[0.04]',
          'transition-[transform,box-shadow] duration-500 ease-out',
          'motion-safe:group-hover/frame:translate-y-[-3px]',
          'motion-safe:group-hover/frame:shadow-[0_32px_64px_-16px_rgba(2,8,20,0.78),0_0_0_1px_rgba(90,143,208,0.2)]',
        )}
      >
        <div className="border-white/[0.06] bg-[#111a28]/95 flex items-center gap-3 border-b px-4 py-2.5">
          <span className="bg-primary/80 size-2 shrink-0 rounded-sm" aria-hidden />
          <span className="text-muted-foreground truncate text-caption font-medium tracking-wide">
            AOVIAS Command
          </span>
          <span className="text-muted-foreground/50 ms-auto hidden text-caption sm:inline">
            {label}
          </span>
        </div>
        <div className="relative aspect-[16/10] overflow-hidden bg-[#0a1018]">
          <div className="size-full transition-transform duration-700 ease-out motion-safe:group-hover/frame:scale-[1.02]">
            {children}
          </div>
        </div>
      </div>
    </div>
  );
}

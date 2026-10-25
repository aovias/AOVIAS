'use client';

import { useEffect, useState } from 'react';

import { cn } from '@/lib/utils';

/**
 * ReadingProgress — sticky article scroll indicator (percentage).
 * Attach to long-form content; observes document scroll by default.
 */
export type ReadingProgressProps = {
  /** CSS selector for the article root; defaults to document scroll. */
  targetSelector?: string;
  className?: string;
  label?: string;
};

export function ReadingProgress({
  targetSelector,
  className,
  label = 'Reading progress',
}: ReadingProgressProps) {
  const [progress, setProgress] = useState(0);

  useEffect(() => {
    const update = () => {
      if (targetSelector) {
        const target = document.querySelector(targetSelector);
        if (!(target instanceof HTMLElement)) {
          setProgress(0);
          return;
        }
        const rect = target.getBoundingClientRect();
        const total = target.offsetHeight - window.innerHeight;
        if (total <= 0) {
          setProgress(rect.bottom <= window.innerHeight ? 100 : 0);
          return;
        }
        const scrolled = Math.min(
          Math.max(-rect.top, 0),
          total,
        );
        setProgress(Math.round((scrolled / total) * 100));
        return;
      }

      const element = document.documentElement;
      const total = element.scrollHeight - element.clientHeight;
      if (total <= 0) {
        setProgress(0);
        return;
      }
      setProgress(Math.round((element.scrollTop / total) * 100));
    };

    update();
    window.addEventListener('scroll', update, { passive: true });
    window.addEventListener('resize', update);
    return () => {
      window.removeEventListener('scroll', update);
      window.removeEventListener('resize', update);
    };
  }, [targetSelector]);

  const clamped = Math.min(100, Math.max(0, progress));

  return (
    <div
      className={cn(
        'bg-background/95 border-border sticky top-0 z-30 border-b backdrop-blur',
        className,
      )}
      role="progressbar"
      aria-label={label}
      aria-valuemin={0}
      aria-valuemax={100}
      aria-valuenow={clamped}
    >
      <div className="bg-muted h-1 w-full">
        <div
          className="bg-primary h-full transition-[width] duration-150 ease-out"
          style={{ width: `${clamped}%` }}
        />
      </div>
      <p className="absolute -m-px size-px overflow-hidden whitespace-nowrap border-0 p-0 [clip:rect(0,0,0,0)]">
        {clamped}% read
      </p>
    </div>
  );
}

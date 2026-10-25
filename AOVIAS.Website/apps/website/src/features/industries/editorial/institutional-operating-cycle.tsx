'use client';

import { useEffect, useRef, useState } from 'react';
import {
  ClipboardCheck,
  Eye,
  FileStack,
  Flag,
  Layers,
  ListChecks,
  RefreshCw,
  Users,
  type LucideIcon,
} from 'lucide-react';

import { cn } from '@/lib/utils';

export type OperatingCycleStep = {
  id: string;
  label: string;
  summary: string;
  icon?: LucideIcon;
};

export type InstitutionalOperatingCycleProps = {
  title: string;
  steps: readonly OperatingCycleStep[];
  className?: string;
};

const DEFAULT_ICONS: LucideIcon[] = [
  Layers,
  Users,
  ListChecks,
  FileStack,
  ClipboardCheck,
  Flag,
  Eye,
  RefreshCw,
];

/**
 * Institutional operating model — continuous governance cycle.
 * Progressive reveal on scroll; enterprise architecture aesthetic (not a flowchart).
 */
export function InstitutionalOperatingCycle({
  title,
  steps,
  className,
}: InstitutionalOperatingCycleProps) {
  const rootRef = useRef<HTMLDivElement>(null);
  const [active, setActive] = useState(false);

  useEffect(() => {
    const node = rootRef.current;
    if (!node) return;

    const reduceMotion = window.matchMedia(
      '(prefers-reduced-motion: reduce)',
    ).matches;
    if (reduceMotion) {
      setActive(true);
      return;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry?.isIntersecting) {
          setActive(true);
          observer.disconnect();
        }
      },
      { threshold: 0.22, rootMargin: '0px 0px -8% 0px' },
    );

    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  return (
    <div
      ref={rootRef}
      data-active={active ? 'true' : 'false'}
      className={cn(
        'ioc-root border-border/60 relative overflow-hidden rounded-2xl border',
        'bg-gradient-to-br from-[#111a2c] via-[#0c121e] to-[#080c14]',
        'shadow-[0_28px_64px_-24px_rgba(2,8,20,0.65)]',
        'px-4 py-8 sm:px-6 sm:py-10 lg:px-10 lg:py-12',
        className,
      )}
    >
      {/* Atmosphere */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-40"
        style={{
          backgroundImage:
            'radial-gradient(ellipse 70% 50% at 50% 0%, rgba(90,143,208,0.18), transparent 60%), radial-gradient(ellipse 50% 40% at 80% 80%, rgba(58,106,158,0.12), transparent 55%)',
        }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-[0.07]"
        style={{
          backgroundImage:
            'linear-gradient(to right, #8fb3e0 1px, transparent 1px), linear-gradient(to bottom, #8fb3e0 1px, transparent 1px)',
          backgroundSize: '40px 40px',
        }}
      />

      <p className="text-domain-accent relative mb-6 text-center text-caption font-semibold tracking-[0.14em] uppercase sm:mb-8">
        Continuous operating cycle
      </p>
      <h3 className="sr-only">{title}</h3>

      {/* Desktop / tablet — architecture spine */}
      <ol className="relative mx-auto hidden max-w-3xl list-none md:block">
        {/* Glow spine */}
        <div
          aria-hidden
          className="ioc-spine absolute top-3 bottom-3 left-1/2 w-px -translate-x-1/2"
        />
        <svg
          aria-hidden
          className="ioc-loop pointer-events-none absolute -bottom-2 left-1/2 h-16 w-40 -translate-x-1/2"
          viewBox="0 0 160 64"
          fill="none"
        >
          <path
            className="ioc-loop-path"
            d="M80 8 V40 C80 52, 52 56, 36 48"
            stroke="url(#ioc-loop-grad)"
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeDasharray="90"
            strokeDashoffset="90"
          />
          <path
            className="ioc-loop-path"
            d="M80 8 V40 C80 52, 108 56, 124 48"
            stroke="url(#ioc-loop-grad)"
            strokeWidth="1.5"
            strokeLinecap="round"
            opacity="0.45"
            strokeDasharray="90"
            strokeDashoffset="90"
          />
          <defs>
            <linearGradient id="ioc-loop-grad" x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#67e8f9" stopOpacity="0.15" />
              <stop offset="1" stopColor="#5a8fd0" stopOpacity="0.7" />
            </linearGradient>
          </defs>
        </svg>

        {steps.map((step, index) => {
          const Icon = step.icon ?? DEFAULT_ICONS[index % DEFAULT_ICONS.length]!;
          const left = index % 2 === 0;
          return (
            <li
              key={step.id}
              className={cn(
                'ioc-step relative grid grid-cols-[1fr_auto_1fr] items-center gap-4 py-3 lg:gap-6 lg:py-3.5',
              )}
              style={{ ['--ioc-i' as string]: index }}
            >
              {/* Left card slot */}
              <div className={cn(left ? 'justify-self-end' : 'justify-self-stretch')}>
                {left ? (
                  <CycleCard step={step} icon={Icon} align="end" />
                ) : (
                  <span className="block" />
                )}
              </div>

              {/* Center node */}
              <div className="ioc-node relative z-[1] flex size-11 shrink-0 items-center justify-center lg:size-12">
                <span className="ioc-node-ring absolute inset-0 rounded-full" />
                <span className="border-border/80 bg-[#121a28] relative flex size-9 items-center justify-center rounded-full border lg:size-10">
                  <Icon
                    className="text-domain-accent size-4 stroke-[1.5] lg:size-[1.125rem]"
                    aria-hidden
                  />
                </span>
              </div>

              {/* Right card slot */}
              <div className={cn(!left ? 'justify-self-start' : 'justify-self-stretch')}>
                {!left ? (
                  <CycleCard step={step} icon={Icon} align="start" />
                ) : (
                  <span className="block" />
                )}
              </div>
            </li>
          );
        })}
      </ol>

      {/* Mobile — stacked premium cards */}
      <ol className="relative mx-auto flex max-w-md list-none flex-col gap-3 md:hidden">
        {steps.map((step, index) => {
          const Icon = step.icon ?? DEFAULT_ICONS[index % DEFAULT_ICONS.length]!;
          return (
            <li
              key={step.id}
              className="ioc-step relative"
              style={{ ['--ioc-i' as string]: index }}
            >
              {index < steps.length - 1 ? (
                <span
                  aria-hidden
                  className="ioc-mobile-connector bg-primary/30 absolute top-full left-6 z-0 h-3 w-px"
                />
              ) : null}
              <CycleCard step={step} icon={Icon} align="start" compact />
            </li>
          );
        })}
      </ol>

      <p className="text-muted-foreground relative mt-8 text-center text-caption leading-relaxed sm:mt-10">
        Governance becomes daily operations — then returns as continuous audit
        readiness.
      </p>
    </div>
  );
}

function CycleCard({
  step,
  icon: Icon,
  align,
  compact,
}: {
  step: OperatingCycleStep;
  icon: LucideIcon;
  align: 'start' | 'end';
  compact?: boolean;
}) {
  return (
    <div
      className={cn(
        'ioc-card border-border/50 group relative max-w-sm overflow-hidden rounded-xl border',
        'bg-[#151e2e]/85 backdrop-blur-sm',
        'shadow-[0_12px_32px_-16px_rgba(2,8,20,0.55)]',
        'ring-1 ring-inset ring-white/[0.04]',
        'transition-[transform,box-shadow,border-color] duration-500',
        'hover:border-primary/35 hover:shadow-[0_16px_40px_-14px_rgba(58,106,158,0.35)]',
        compact ? 'p-4' : 'px-5 py-4 lg:px-6 lg:py-5',
        align === 'end' && 'text-right',
      )}
    >
      <div
        className={cn(
          'flex items-start gap-3',
          align === 'end' && 'flex-row-reverse',
          compact && 'items-center',
        )}
      >
        <span
          className={cn(
            'bg-primary/12 text-domain-accent flex shrink-0 items-center justify-center rounded-lg',
            compact ? 'size-9' : 'size-10',
            'md:hidden',
          )}
        >
          <Icon className="size-4 stroke-[1.5]" aria-hidden />
        </span>
        <div className="min-w-0">
          <p className="text-foreground text-small font-semibold tracking-tight lg:text-body">
            {step.label}
          </p>
          <p
            className={cn(
              'text-muted-foreground mt-1 text-caption leading-snug',
              align === 'end' && 'ml-auto',
            )}
          >
            {step.summary}
          </p>
        </div>
      </div>
    </div>
  );
}

'use client';

import { useEffect, useRef, useState } from 'react';
import {
  ClipboardCheck,
  Eye,
  FileStack,
  Layers,
  ListChecks,
  RefreshCw,
  Shield,
  type LucideIcon,
} from 'lucide-react';

import { cn } from '@/lib/utils';

export type TransformationOutcome = {
  id: string;
  title: string;
  description: string;
};

export type TransformationFlowStep = {
  id: string;
  label: string;
};

export type InstitutionalTransformationBandProps = {
  outcomes: readonly TransformationOutcome[];
  flowSteps: readonly TransformationFlowStep[];
  strip: readonly string[];
  className?: string;
};

const FLOW_ICONS: LucideIcon[] = [
  Layers,
  ListChecks,
  FileStack,
  ClipboardCheck,
  Shield,
  Eye,
  RefreshCw,
];

/**
 * Executive transformation briefing — compact outcome cards (40%) +
 * primary workflow illustration (60%) + integrated outcome strip.
 */
export function InstitutionalTransformationBand({
  outcomes,
  flowSteps,
  strip,
  className,
}: InstitutionalTransformationBandProps) {
  const diagramRef = useRef<HTMLDivElement>(null);
  const [active, setActive] = useState(false);

  useEffect(() => {
    const node = diagramRef.current;
    if (!node) return;

    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
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
      { threshold: 0.2, rootMargin: '0px 0px -6% 0px' },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  return (
    <div
      className={cn(
        'border-border/55 overflow-hidden rounded-2xl border',
        'bg-card/20 shadow-surface ring-1 ring-inset ring-white/[0.03]',
        className,
      )}
    >
      {/* 40% / 60% — shared top baseline */}
      <div className="grid items-start gap-6 p-4 sm:gap-8 sm:p-6 lg:grid-cols-[2fr_3fr] lg:gap-10 lg:p-8 xl:gap-12">
        {/* Left — compact transformation cards */}
        <ul className="flex list-none flex-col gap-3 pt-0">
          {outcomes.map((item) => (
            <li
              key={item.id}
              className={cn(
                'border-border/55 bg-card/55 group relative overflow-hidden rounded-xl border',
                'px-3.5 py-3 sm:px-4 sm:py-3.5',
                'shadow-surface ring-1 ring-inset ring-white/[0.03]',
                'transition-[transform,box-shadow,border-color] duration-500',
                'hover:border-primary/35 hover:shadow-raised',
              )}
            >
              <span
                aria-hidden
                className="bg-domain-accent/80 absolute top-0 left-0 h-full w-0.5"
              />
              <p className="text-domain-accent text-caption mb-1 font-semibold tracking-[0.1em] uppercase">
                After AOVIAS
              </p>
              <h3 className="text-foreground text-small font-semibold tracking-tight sm:text-body">
                {item.title}
              </h3>
              <p className="text-muted-foreground mt-1.5 text-caption leading-snug sm:leading-relaxed">
                {item.description}
              </p>
            </li>
          ))}
        </ul>

        {/* Right — visual focal workflow */}
        <div
          ref={diagramRef}
          data-active={active ? 'true' : 'false'}
          className={cn(
            'itb-diagram border-border/50 relative min-h-[28rem] overflow-hidden rounded-xl border sm:min-h-[32rem] lg:min-h-[36rem]',
            'bg-gradient-to-br from-[#111a2c] via-[#0c121e] to-[#080c14]',
            'shadow-[0_20px_48px_-18px_rgba(2,8,20,0.55)]',
            'flex flex-col px-4 py-5 sm:px-6 sm:py-6 lg:px-8 lg:py-7',
          )}
        >
          <div
            aria-hidden
            className="pointer-events-none absolute inset-0 opacity-40"
            style={{
              backgroundImage:
                'radial-gradient(ellipse 70% 50% at 50% 15%, rgba(90,143,208,0.22), transparent 62%)',
            }}
          />
          <div
            aria-hidden
            className="pointer-events-none absolute inset-0 opacity-[0.07]"
            style={{
              backgroundImage:
                'linear-gradient(to right, #8fb3e0 1px, transparent 1px), linear-gradient(to bottom, #8fb3e0 1px, transparent 1px)',
              backgroundSize: '36px 36px',
            }}
          />

          <div className="relative mb-4 shrink-0 text-center sm:mb-5">
            <p className="text-domain-accent text-caption font-semibold tracking-[0.12em] uppercase">
              Operational transformation
            </p>
            <p className="text-muted-foreground mt-1 text-caption">
              Fragmented activity → connected institutional platform
            </p>
          </div>

          <div className="itb-hub relative z-[1] mx-auto mb-4 flex w-full max-w-xs shrink-0 items-center justify-center gap-2 rounded-full border border-[#5a8fd0]/40 bg-[#1a3050]/55 px-5 py-2.5 backdrop-blur-sm sm:mb-5">
            <span className="bg-primary size-1.5 rounded-full shadow-[0_0_8px_rgba(90,143,208,0.8)]" aria-hidden />
            <span className="text-foreground text-caption font-semibold tracking-wide sm:text-small">
              AOVIAS Command
            </span>
            <span className="text-muted-foreground hidden text-caption sm:inline">
              · orchestration layer
            </span>
          </div>

          {/* Workflow — fills remaining height */}
          <ol className="relative z-[1] mx-auto flex w-full max-w-md flex-1 list-none flex-col justify-between py-1">
            {flowSteps.map((step, index) => {
              const Icon = FLOW_ICONS[index % FLOW_ICONS.length]!;
              const isLast = index === flowSteps.length - 1;
              return (
                <li
                  key={step.id}
                  className="itb-step relative flex flex-col items-center"
                  style={{ ['--itb-i' as string]: index }}
                >
                  <div
                    className={cn(
                      'border-border/55 relative z-[1] flex w-full items-center gap-3.5 rounded-xl border px-4 py-3.5 sm:px-5 sm:py-4',
                      'bg-[#151e2e]/92 backdrop-blur-sm',
                      'shadow-[0_12px_32px_-14px_rgba(2,8,20,0.6)]',
                      'ring-1 ring-inset ring-white/[0.05]',
                      'transition-[border-color,box-shadow] duration-500',
                      'hover:border-primary/40',
                    )}
                  >
                    <span className="bg-primary/15 text-domain-accent flex size-9 shrink-0 items-center justify-center rounded-lg sm:size-10">
                      <Icon className="size-4 stroke-[1.5] sm:size-[1.125rem]" aria-hidden />
                    </span>
                    <span className="text-foreground text-small font-semibold tracking-tight sm:text-body">
                      {step.label}
                    </span>
                    <span className="text-muted-foreground/50 ms-auto text-caption tabular-nums">
                      {String(index + 1).padStart(2, '0')}
                    </span>
                  </div>

                  {!isLast ? (
                    <div
                      aria-hidden
                      className="itb-connector relative z-0 flex h-5 w-full items-center justify-center sm:h-6 lg:h-7"
                    >
                      <svg
                        className="h-full w-6 overflow-visible"
                        viewBox="0 0 24 28"
                        fill="none"
                      >
                        <path
                          className="itb-connector-path"
                          d="M12 2 V22"
                          stroke="url(#itb-conn-grad)"
                          strokeWidth="1.5"
                          strokeLinecap="round"
                          strokeDasharray="4 5"
                        />
                        <circle
                          className="itb-connector-dot"
                          cx="12"
                          cy="14"
                          r="2.2"
                          fill="#7eb8e8"
                        />
                        <path
                          d="M8 20 L12 24 L16 20"
                          stroke="#5a8fd0"
                          strokeWidth="1.25"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          opacity="0.7"
                        />
                      </svg>
                    </div>
                  ) : null}
                </li>
              );
            })}
            <svg width="0" height="0" aria-hidden className="absolute">
              <defs>
                <linearGradient id="itb-conn-grad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0" stopColor="#67e8f9" stopOpacity="0.15" />
                  <stop offset="0.5" stopColor="#5a8fd0" stopOpacity="0.85" />
                  <stop offset="1" stopColor="#5a8fd0" stopOpacity="0.25" />
                </linearGradient>
              </defs>
            </svg>
          </ol>

          <p className="text-muted-foreground relative mt-4 shrink-0 text-center text-caption leading-snug sm:mt-5">
            Command orchestrates the operating chain — from policy to continuous
            readiness.
          </p>
        </div>
      </div>

      {/* Integrated outcome strip — section footer */}
      <ul
        className={cn(
          'border-border/50 flex list-none flex-wrap items-stretch justify-between gap-y-2',
          'border-t bg-[#0c121e]/55 px-3 py-3.5 sm:px-6 sm:py-4',
        )}
      >
        {strip.map((label, index) => (
          <li
            key={label}
            className={cn(
              'text-foreground flex min-w-[7rem] flex-1 items-center justify-center gap-2 px-2 py-1 text-center text-caption font-medium',
              index > 0 && 'border-border/35 sm:border-l',
            )}
          >
            <span
              className="bg-domain-accent/90 size-1.5 shrink-0 rounded-full"
              aria-hidden
            />
            {label}
          </li>
        ))}
      </ul>
    </div>
  );
}

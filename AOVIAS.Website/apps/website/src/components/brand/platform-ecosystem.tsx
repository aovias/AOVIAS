'use client';

import { useEffect, useRef, useState } from 'react';

import { cn } from '@/lib/utils';

export type PlatformEcosystemProps = {
  title?: string;
  caption?: string;
  className?: string;
};

/**
 * Signature ecosystem ladder:
 * Society → People → Organizations → Products → Shared Services → AOVIAS Platform
 * Bars widen upward; Platform width is fixed (narrowest).
 * Scroll-once reveal: Platform → … → Society.
 */
export function PlatformEcosystem({
  title = 'AOVIAS platform stack illustration',
  caption,
  className,
}: PlatformEcosystemProps) {
  const rootRef = useRef<HTMLDivElement>(null);
  const [active, setActive] = useState(false);

  useEffect(() => {
    const node = rootRef.current;
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
      { threshold: 0.28, rootMargin: '0px 0px -8% 0px' },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  /** Centered at x=400. Platform kept at 300; each layer above grows wider. */
  const layers: {
    y: number;
    w: number;
    label: string;
    fill: string;
    stroke: string;
    strokeOpacity: number;
    strokeWidth: number;
    textFill: string;
    weight: number;
    letterSpacing: number;
    highlightOpacity: number;
  }[] = [
    {
      y: 96,
      w: 580,
      label: 'Society',
      fill: 'url(#pe-bar1)',
      stroke: '#4a5776',
      strokeOpacity: 0.5,
      strokeWidth: 0.5,
      textFill: '#c5d0e4',
      weight: 500,
      letterSpacing: 0.2,
      highlightOpacity: 0.04,
    },
    {
      y: 180,
      w: 520,
      label: 'People',
      fill: 'url(#pe-bar2)',
      stroke: '#4a5776',
      strokeOpacity: 0.55,
      strokeWidth: 0.5,
      textFill: '#d0daf0',
      weight: 500,
      letterSpacing: 0.2,
      highlightOpacity: 0.05,
    },
    {
      y: 264,
      w: 460,
      label: 'Organizations',
      fill: 'url(#pe-bar3)',
      stroke: '#556488',
      strokeOpacity: 0.55,
      strokeWidth: 0.6,
      textFill: '#d8e2f4',
      weight: 550,
      letterSpacing: 0.2,
      highlightOpacity: 0.06,
    },
    {
      y: 348,
      w: 400,
      label: 'Products · Command · Studio',
      fill: 'url(#pe-bar4)',
      stroke: '#5a6f96',
      strokeOpacity: 0.6,
      strokeWidth: 0.7,
      textFill: '#e2eaf8',
      weight: 600,
      letterSpacing: 0.15,
      highlightOpacity: 0.08,
    },
    {
      y: 432,
      w: 350,
      label: 'Shared Platform Services',
      fill: 'url(#pe-bar5)',
      stroke: '#6a82ab',
      strokeOpacity: 0.65,
      strokeWidth: 0.8,
      textFill: '#e8eef8',
      weight: 650,
      letterSpacing: 0.2,
      highlightOpacity: 0.1,
    },
    {
      y: 516,
      w: 300,
      label: 'AOVIAS Platform',
      fill: 'url(#pe-bar6)',
      stroke: '#8fb3e0',
      strokeOpacity: 0.7,
      strokeWidth: 1,
      textFill: '#f2f6fc',
      weight: 700,
      letterSpacing: 0.3,
      highlightOpacity: 0.12,
    },
  ];

  const layerCount = layers.length;

  return (
    <div
      ref={rootRef}
      role="img"
      aria-label={title}
      data-active={active ? 'true' : 'false'}
      className={cn(
        'pe-ecosystem relative w-full overflow-hidden rounded-2xl',
        'shadow-[0_0_24px_rgba(74,114,173,0.28)]',
        'aspect-[4/3] min-h-[14rem] xl:min-h-[20rem]',
        className,
      )}
    >
      <svg
        viewBox="0 0 800 600"
        xmlns="http://www.w3.org/2000/svg"
        className="size-full"
        aria-hidden
      >
        <title>{title}</title>
        <desc>
          Six tapering layers labelled Society, People, Organizations, Products
          · Command · Studio, Shared Platform Services, and AOVIAS Platform,
          connected by a vertical spine with junction nodes and a glowing core
          beneath the platform layer, framed by corner marks on a dark navy
          card.
        </desc>
        <defs>
          <linearGradient id="pe-bgGrad" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#111a2c" />
            <stop offset="1" stopColor="#0a0e17" />
          </linearGradient>
          <radialGradient id="pe-topGlow" cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#4a72ad" stopOpacity="0.14" />
            <stop offset="1" stopColor="#4a72ad" stopOpacity="0" />
          </radialGradient>
          <radialGradient id="pe-coreGlow" cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#6f93c2" stopOpacity="0.3" />
            <stop offset="1" stopColor="#6f93c2" stopOpacity="0" />
          </radialGradient>
          <radialGradient id="pe-nodeGlow" cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#a8cbf2" stopOpacity="0.6" />
            <stop offset="1" stopColor="#a8cbf2" stopOpacity="0" />
          </radialGradient>
          <linearGradient id="pe-spineGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#8fb3e0" stopOpacity="0.15" />
            <stop offset="1" stopColor="#8fb3e0" stopOpacity="0.55" />
          </linearGradient>
          <linearGradient id="pe-bar1" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#2a3349" />
            <stop offset="1" stopColor="#222a3d" />
          </linearGradient>
          <linearGradient id="pe-bar2" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#2c3650" />
            <stop offset="1" stopColor="#242e44" />
          </linearGradient>
          <linearGradient id="pe-bar3" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#303c58" />
            <stop offset="1" stopColor="#28324a" />
          </linearGradient>
          <linearGradient id="pe-bar4" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#354263" />
            <stop offset="1" stopColor="#2b3752" />
          </linearGradient>
          <linearGradient id="pe-bar5" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#3b4a6f" />
            <stop offset="1" stopColor="#2f3c5a" />
          </linearGradient>
          <linearGradient id="pe-bar6" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#5578ae" />
            <stop offset="1" stopColor="#33436c" />
          </linearGradient>
          <pattern
            id="pe-dots"
            width="28"
            height="28"
            patternUnits="userSpaceOnUse"
          >
            <circle cx="1" cy="1" r="1" fill="#8fb3e0" fillOpacity="0.07" />
          </pattern>
          <clipPath id="pe-cardClip">
            <rect x="0" y="0" width="800" height="600" rx="24" />
          </clipPath>
          <filter id="pe-softShadow" x="-30%" y="-30%" width="160%" height="200%">
            <feDropShadow
              dx="0"
              dy="6"
              stdDeviation="8"
              floodColor="#000000"
              floodOpacity="0.35"
            />
          </filter>
        </defs>

        <g clipPath="url(#pe-cardClip)">
          <rect x="0" y="0" width="800" height="600" fill="url(#pe-bgGrad)" />
          <rect x="0" y="0" width="800" height="600" fill="url(#pe-dots)" />
          <circle cx="400" cy="30" r="260" fill="url(#pe-topGlow)" />
          <circle cx="400" cy="546" r="200" fill="url(#pe-coreGlow)" />

          <line
            x1="400"
            y1="144"
            x2="400"
            y2="180"
            stroke="url(#pe-spineGrad)"
            strokeWidth="1.5"
          />
          <line
            x1="400"
            y1="228"
            x2="400"
            y2="264"
            stroke="url(#pe-spineGrad)"
            strokeWidth="1.5"
          />
          <line
            x1="400"
            y1="312"
            x2="400"
            y2="348"
            stroke="url(#pe-spineGrad)"
            strokeWidth="1.5"
          />
          <line
            x1="400"
            y1="396"
            x2="400"
            y2="432"
            stroke="url(#pe-spineGrad)"
            strokeWidth="1.5"
          />
          <line
            x1="400"
            y1="480"
            x2="400"
            y2="516"
            stroke="url(#pe-spineGrad)"
            strokeWidth="1.5"
          />
          <line
            x1="400"
            y1="564"
            x2="400"
            y2="578"
            stroke="url(#pe-spineGrad)"
            strokeWidth="1.5"
          />

          <circle cx="400" cy="144" r="3" fill="#8fb3e0" opacity="0.55" />
          <circle cx="400" cy="228" r="3" fill="#8fb3e0" opacity="0.6" />
          <circle cx="400" cy="312" r="3.2" fill="#8fb3e0" opacity="0.65" />
          <circle cx="400" cy="396" r="3.4" fill="#8fb3e0" opacity="0.7" />
          <circle cx="400" cy="480" r="3.6" fill="#8fb3e0" opacity="0.75" />

          <circle cx="400" cy="582" r="22" fill="url(#pe-nodeGlow)" />
          <circle
            cx="400"
            cy="582"
            r="9"
            fill="none"
            stroke="#a8cbf2"
            strokeWidth="1"
            opacity="0.6"
          />
          <circle cx="400" cy="582" r="3.5" fill="#dbe9fb" />

          {layers.map((layer, index) => {
            const x = 400 - layer.w / 2;
            /** Reveal Platform first (bottom), Society last (top). */
            const revealOrder = layerCount - 1 - index;
            return (
              <g
                key={layer.label}
                className="pe-layer"
                style={{ ['--pe-i' as string]: revealOrder }}
                filter="url(#pe-softShadow)"
              >
                <rect
                  x={x}
                  y={layer.y}
                  width={layer.w}
                  height={48}
                  rx={10}
                  fill={layer.fill}
                  stroke={layer.stroke}
                  strokeWidth={layer.strokeWidth}
                  strokeOpacity={layer.strokeOpacity}
                />
                <rect
                  x={x}
                  y={layer.y}
                  width={layer.w}
                  height={1}
                  fill="#ffffff"
                  opacity={layer.highlightOpacity}
                />
                <text
                  x={400}
                  y={layer.y + 30}
                  textAnchor="middle"
                  fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
                  fontSize={15}
                  fontWeight={layer.weight}
                  fill={layer.textFill}
                  letterSpacing={layer.letterSpacing}
                >
                  {layer.label}
                </text>
              </g>
            );
          })}

          <g stroke="#ffffff" strokeOpacity="0.16" strokeWidth="1">
            <path d="M20,44 L20,20 L44,20" fill="none" />
            <path d="M780,44 L780,20 L756,20" fill="none" />
            <path d="M20,556 L20,580 L44,580" fill="none" />
            <path d="M780,556 L780,580 L756,580" fill="none" />
          </g>

          <rect
            x="0.5"
            y="0.5"
            width="799"
            height="599"
            rx="23.5"
            fill="none"
            stroke="#3a4a66"
            strokeOpacity="0.55"
          />
        </g>
      </svg>
      {caption ? (
        <p className="text-muted-foreground absolute inset-x-0 bottom-3 px-4 text-center text-caption">
          {caption}
        </p>
      ) : null}
    </div>
  );
}

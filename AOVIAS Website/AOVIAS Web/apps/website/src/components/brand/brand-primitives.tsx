import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

/** Stroke / fill ladder shared by the AOVIAS illustration family. */
export const brandInk = {
  strokeHair: 1,
  strokeThin: 1.25,
  stroke: 1.5,
  strokeStrong: 2,
  radius: 6,
  radiusLg: 8,
  /** Opacities tuned so brand hue is unmistakable (not grey washes) */
  fillFaint: 0.14,
  fillSoft: 0.24,
  fillMid: 0.36,
  fillStrong: 0.52,
  fillAccent: 0.68,
  strokeMuted: 0.55,
  strokeMid: 0.85,
  label: 1,
} as const;

export type BrandShellProps = {
  title: string;
  className?: string;
  aspectClassName?: string;
  children: ReactNode;
  caption?: string;
};

/**
 * Shared chrome for brand illustrations — elevated enterprise frame, a11y label.
 */
export function BrandShell({
  title,
  className,
  aspectClassName = 'min-h-[12rem]',
  children,
  caption,
}: BrandShellProps) {
  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'border-border from-card via-card to-primary-muted/40 text-illustration-ink shadow-raised relative flex size-full flex-col overflow-hidden rounded-xl border bg-gradient-to-br',
        'before:from-domain-accent before:via-domain-accent/50 before:absolute before:inset-x-0 before:top-0 before:h-0.5 before:bg-gradient-to-r before:to-transparent',
        'after:pointer-events-none after:absolute after:inset-0 after:bg-gradient-to-tr after:from-domain-accent/5 after:via-transparent after:to-secondary-muted/30',
        aspectClassName,
        className,
      )}
    >
      {children}
      {caption ? (
        <p className="text-muted-foreground relative z-10 px-4 pb-4 text-small font-medium">
          {caption}
        </p>
      ) : null}
    </div>
  );
}

export type BrandGridProps = {
  width?: number;
  height?: number;
  step?: number;
  opacity?: number;
};

/** Blueprint grid module — light/dark via currentColor. */
export function BrandGrid({
  width = 400,
  height = 300,
  step = 24,
  opacity = 0.08,
}: BrandGridProps) {
  const lines: ReactNode[] = [];
  for (let x = 0; x <= width; x += step) {
    lines.push(
      <line
        key={`v-${x}`}
        x1={x}
        y1={0}
        x2={x}
        y2={height}
        stroke="currentColor"
        strokeWidth={brandInk.strokeHair}
        opacity={opacity}
      />,
    );
  }
  for (let y = 0; y <= height; y += step) {
    lines.push(
      <line
        key={`h-${y}`}
        x1={0}
        y1={y}
        x2={width}
        y2={y}
        stroke="currentColor"
        strokeWidth={brandInk.strokeHair}
        opacity={opacity}
      />,
    );
  }
  return <g aria-hidden>{lines}</g>;
}

export type BrandPanelProps = {
  x: number;
  y: number;
  width: number;
  height: number;
  opacity?: number;
  radius?: number;
  stroked?: boolean;
};

/** Architectural panel — filled plane with optional hairline stroke. */
export function BrandPanel({
  x,
  y,
  width,
  height,
  opacity = brandInk.fillMid,
  radius = brandInk.radius,
  stroked = false,
}: BrandPanelProps) {
  return (
    <rect
      x={x}
      y={y}
      width={width}
      height={height}
      rx={radius}
      fill="currentColor"
      opacity={opacity}
      stroke={stroked ? 'currentColor' : undefined}
      strokeWidth={stroked ? brandInk.strokeHair : undefined}
      strokeOpacity={stroked ? brandInk.strokeMuted : undefined}
    />
  );
}

export type BrandNodeProps = {
  cx: number;
  cy: number;
  r?: number;
  opacity?: number;
};

/** Hub / endpoint node for ecosystem diagrams. */
export function BrandNode({
  cx,
  cy,
  r = 8,
  opacity = brandInk.fillStrong,
}: BrandNodeProps) {
  return (
    <circle
      cx={cx}
      cy={cy}
      r={r}
      fill="currentColor"
      opacity={opacity}
      stroke="currentColor"
      strokeWidth={brandInk.strokeThin}
      strokeOpacity={brandInk.strokeMid}
    />
  );
}

export type BrandConnectorProps = {
  d: string;
  opacity?: number;
  width?: number;
};

/** Structural connector path. */
export function BrandConnector({
  d,
  opacity = brandInk.strokeMid,
  width = brandInk.strokeThin,
}: BrandConnectorProps) {
  return (
    <path
      d={d}
      fill="none"
      stroke="currentColor"
      strokeWidth={width}
      opacity={opacity}
      strokeLinecap="round"
      strokeLinejoin="round"
    />
  );
}

export type BrandLabelProps = {
  x: number;
  y: number;
  children: string;
  size?: number;
  anchor?: 'start' | 'middle' | 'end';
  opacity?: number;
};

/** Diagram label — restrained typography inside SVG. */
export function BrandLabel({
  x,
  y,
  children,
  size = 10,
  anchor = 'middle',
  opacity = brandInk.label,
}: BrandLabelProps) {
  return (
    <text
      x={x}
      y={y}
      textAnchor={anchor}
      fill="currentColor"
      opacity={opacity}
      style={{ fontSize: size, fontWeight: 600 }}
    >
      {children}
    </text>
  );
}

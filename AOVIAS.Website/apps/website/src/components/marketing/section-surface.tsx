import type { ReactNode } from 'react';

import {
  BrandAtmosphere,
  type EnterpriseAtmosphereVariant,
} from '@/components/brand';
import { Container } from '@/components/primitives';
import { cn } from '@/lib/utils';
import type { AvlfAtmosphereId } from '@/registries/avlf';
import { resolveAtmosphereVariant } from '@/registries/avlf/atmosphere';

/**
 * Marketing section surface tokens — enterprise color rhythm.
 * Use for alternating bands without inventing one-off background classes.
 */
export type SectionSurfaceTone =
  | 'canvas'
  | 'muted'
  | 'subtle'
  | 'elevated'
  | 'feature'
  | 'tint'
  | 'blueprint'
  | 'contrast';

export type SectionSurfaceProps = {
  children: ReactNode;
  tone?: SectionSurfaceTone;
  /**
   * Optional EBAS atmosphere for mid-page bands (AVLS or live key).
   * Prefer named assets over the legacy `blueprint` tone grid.
   */
  atmosphere?: AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  /** Optional landmark label. */
  'aria-label'?: string;
  id?: string;
  className?: string;
  /** Container width — default wide for large-screen immersion. */
  width?: 'default' | 'wide' | 'narrow' | 'full';
  /** Vertical padding scale. */
  spacing?: 'md' | 'lg' | 'xl';
  bordered?: boolean;
};

const toneClass: Record<SectionSurfaceTone, string> = {
  canvas: 'bg-background',
  muted: 'bg-surface-muted',
  subtle: 'bg-surface-subtle',
  elevated: 'bg-surface-elevated shadow-surface',
  feature:
    'bg-gradient-to-b from-surface-section via-card to-surface-muted/70',
  tint: 'bg-primary-muted/45',
  /* Legacy engineering band — prefer atmosphere="blueprint-grid" for new work. */
  blueprint: 'bg-surface-section',
  contrast: 'bg-primary-muted/55',
};

const spacingClass: Record<NonNullable<SectionSurfaceProps['spacing']>, string> = {
  md: 'py-section-y-md',
  lg: 'py-section-y-lg',
  xl: 'py-section-y-xl',
};

export function SectionSurface({
  children,
  tone = 'canvas',
  atmosphere,
  id,
  className,
  width = 'wide',
  spacing = 'lg',
  bordered = false,
  'aria-label': ariaLabel,
}: SectionSurfaceProps) {
  const resolvedAtmosphere = atmosphere
    ? resolveAtmosphereVariant(atmosphere, 'blueprint-grid')
    : tone === 'blueprint'
      ? 'blueprint-grid'
      : undefined;

  return (
    <section
      id={id}
      aria-label={ariaLabel}
      className={cn(
        'relative w-full overflow-hidden',
        resolvedAtmosphere ? 'bg-transparent' : toneClass[tone],
        spacingClass[spacing],
        bordered && 'border-border border-y',
        className,
      )}
    >
      {resolvedAtmosphere ? (
        <BrandAtmosphere
          variant={resolvedAtmosphere}
          density="subtle"
          className="opacity-90"
        />
      ) : null}
      <Container width={width} className="relative">
        {children}
      </Container>
    </section>
  );
}

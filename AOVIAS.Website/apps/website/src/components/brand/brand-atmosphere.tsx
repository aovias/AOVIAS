import {
  EnterpriseAtmosphere,
  type EnterpriseAtmosphereVariant,
} from './enterprise-atmosphere';
import type { AvlfAtmosphereId } from '@/registries/avlf';
import { resolveAtmosphereVariant } from '@/registries/avlf/atmosphere';

export type BrandAtmosphereProps = {
  className?: string;
  /** Soften the right-side glow when a hero illustration occupies that column. */
  quietRight?: boolean;
  /**
   * EBAS background asset — AVLS ID or live EnterpriseAtmosphere key.
   * Blueprint remains the compatibility default.
   */
  variant?: AvlfAtmosphereId | EnterpriseAtmosphereVariant;
  /** Density of the selected atmosphere. */
  density?: 'subtle' | 'default' | 'rich';
};

/**
 * Compatibility entry point for existing brand surfaces.
 * New page heroes should declare an explicit EBAS `variant`.
 */
export function BrandAtmosphere({
  className,
  quietRight = false,
  variant = 'blueprint-grid',
  density = 'default',
}: BrandAtmosphereProps) {
  const resolved = resolveAtmosphereVariant(variant, 'blueprint-grid');

  return (
    <EnterpriseAtmosphere
      variant={resolved}
      quietRight={quietRight}
      density={density}
      {...(className ? { className } : {})}
    />
  );
}

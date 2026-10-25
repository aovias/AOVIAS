'use client';

import {
  createContext,
  useContext,
  type ReactNode,
} from 'react';

import type { EnterpriseAtmosphereVariant } from '@/components/brand';
import type { AvlfAtmosphereId } from '@/registries/avlf';
import {
  resolveAvlfAtmosphere,
  resolveAtmosphereVariant,
} from '@/registries/avlf/atmosphere';

export type LayoutAtmosphereContextValue = {
  /** AVLS atmosphere requested by the active layout. */
  atmosphereId: AvlfAtmosphereId;
  /** Resolved live EnterpriseAtmosphere key. */
  atmosphereVariant: EnterpriseAtmosphereVariant;
};

const LayoutAtmosphereContext = createContext<LayoutAtmosphereContextValue | null>(
  null,
);

export type LayoutAtmosphereProviderProps = {
  atmosphere: AvlfAtmosphereId;
  children: ReactNode;
};

export function LayoutAtmosphereProvider({
  atmosphere,
  children,
}: LayoutAtmosphereProviderProps) {
  const value: LayoutAtmosphereContextValue = {
    atmosphereId: atmosphere,
    atmosphereVariant: resolveAvlfAtmosphere(atmosphere),
  };

  return (
    <LayoutAtmosphereContext.Provider value={value}>
      {children}
    </LayoutAtmosphereContext.Provider>
  );
}

export function useLayoutAtmosphere(): LayoutAtmosphereContextValue | null {
  return useContext(LayoutAtmosphereContext);
}

/**
 * Resolve hero/section atmosphere against layout default.
 * `true` → layout atmosphere (or blueprint fallback).
 */
export function useResolvedAtmosphere(
  atmosphere: boolean | AvlfAtmosphereId | EnterpriseAtmosphereVariant | undefined,
  fallback: EnterpriseAtmosphereVariant = 'blueprint-grid',
): EnterpriseAtmosphereVariant | null {
  const layout = useLayoutAtmosphere();
  if (atmosphere === false) return null;
  if (atmosphere === true || atmosphere === undefined) {
    return layout?.atmosphereVariant ?? fallback;
  }
  return resolveAtmosphereVariant(atmosphere, layout?.atmosphereVariant ?? fallback);
}

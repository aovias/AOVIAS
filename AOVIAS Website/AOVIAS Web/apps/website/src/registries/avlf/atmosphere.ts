import {
  avlfAtmosphereCompatibility,
  avlfAtmosphereIds,
  type AvlfAtmosphereId,
} from './catalog';

import type { EnterpriseAtmosphereVariant } from '@/components/brand/enterprise-atmosphere';

/** Layout family → AVLS atmosphere (EBAS). */
export const layoutAtmosphereDefaults = {
  marketing: 'enterprise',
  product: 'workflow',
  technology: 'blueprint',
  knowledge: 'knowledge',
  company: 'corporate',
  solutions: 'corporate',
  documentation: 'knowledge',
  legal: 'minimal',
  authentication: 'security',
  error: 'minimal',
} as const satisfies Record<string, AvlfAtmosphereId>;

export type LayoutAtmosphereFamily = keyof typeof layoutAtmosphereDefaults;

const avlfIdSet = new Set<string>(avlfAtmosphereIds);

export function isAvlfAtmosphereId(value: string): value is AvlfAtmosphereId {
  return avlfIdSet.has(value);
}

/**
 * Resolve an AVLS atmosphere ID to the live EnterpriseAtmosphere key.
 */
export function resolveAvlfAtmosphere(
  id: AvlfAtmosphereId,
): EnterpriseAtmosphereVariant {
  return avlfAtmosphereCompatibility[id] as EnterpriseAtmosphereVariant;
}

/**
 * Accept AVLS IDs or live EnterpriseAtmosphere keys.
 * `true` / undefined → layout fallback or blueprint compatibility default.
 */
export function resolveAtmosphereVariant(
  input: AvlfAtmosphereId | EnterpriseAtmosphereVariant | boolean | undefined,
  fallback: EnterpriseAtmosphereVariant = 'blueprint-grid',
): EnterpriseAtmosphereVariant {
  if (input === true || input === undefined) {
    return fallback;
  }
  if (input === false) {
    return fallback;
  }
  if (isAvlfAtmosphereId(input)) {
    return resolveAvlfAtmosphere(input);
  }
  return input;
}

export function layoutAtmosphereVariant(
  family: LayoutAtmosphereFamily,
): EnterpriseAtmosphereVariant {
  return resolveAvlfAtmosphere(layoutAtmosphereDefaults[family]);
}

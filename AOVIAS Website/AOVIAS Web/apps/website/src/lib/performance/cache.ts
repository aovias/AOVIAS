import { cachePolicies } from '@/config/performance';

export type CachePolicyName = keyof typeof cachePolicies;

/**
 * Resolve a named Cache-Control policy for headers / CDN config.
 */
export function getCacheControl(policy: CachePolicyName): string {
  return cachePolicies[policy];
}

/**
 * Header tuples for Next.js `headers()` config.
 */
export function cacheControlHeader(policy: CachePolicyName): {
  key: string;
  value: string;
} {
  return {
    key: 'Cache-Control',
    value: getCacheControl(policy),
  };
}

export { cachePolicies };

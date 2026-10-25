/**
 * Event taxonomy namespaces — keep names consistent across the AOVIAS ecosystem.
 *
 * Pattern: `{namespace}.{verb}` or `{namespace}.{object}.{verb}`
 */

export const EVENT_NAMESPACES = [
  'navigation',
  'page',
  'product',
  'technology',
  'pricing',
  'contact',
  'career',
  'search',
  'resource',
  'conversion',
  'identity',
  'commerce',
  'error',
] as const;

export type EventNamespace = (typeof EVENT_NAMESPACES)[number];

export function assertEventNameConvention(name: string): boolean {
  const [namespace] = name.split('.');
  return (
    typeof namespace === 'string' &&
    (EVENT_NAMESPACES as readonly string[]).includes(namespace)
  );
}

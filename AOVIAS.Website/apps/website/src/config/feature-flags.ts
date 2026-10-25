/**
 * Feature flags for integration readiness and future Release 2 toggles.
 * Resolved from environment — no remote flag service in R1.
 */

export type FeatureFlags = {
  /** Use mock SDK adapters (always true in R1). */
  useMockSdks: boolean;
  /** Future: enable live Identity Platform. */
  identityLive: boolean;
  /** Future: enable live Commerce API. */
  commerceLive: boolean;
  /** Future: enable live Contact delivery. */
  contactLive: boolean;
  /** Future: enable live Knowledge CMS. */
  knowledgeLive: boolean;
  /** Future: enable hosted search. */
  searchLive: boolean;
  /** Future: analytics collectors. */
  analyticsEnabled: boolean;
};

function flag(value: string | undefined, fallback: boolean): boolean {
  if (value === undefined || value === '') return fallback;
  return value === '1' || value.toLowerCase() === 'true';
}

export function resolveFeatureFlags(): FeatureFlags {
  return {
    useMockSdks: flag(process.env['NEXT_PUBLIC_FF_USE_MOCK_SDKS'], true),
    identityLive: flag(process.env['NEXT_PUBLIC_FF_IDENTITY_LIVE'], false),
    commerceLive: flag(process.env['NEXT_PUBLIC_FF_COMMERCE_LIVE'], false),
    contactLive: flag(process.env['NEXT_PUBLIC_FF_CONTACT_LIVE'], false),
    knowledgeLive: flag(process.env['NEXT_PUBLIC_FF_KNOWLEDGE_LIVE'], false),
    searchLive: flag(process.env['NEXT_PUBLIC_FF_SEARCH_LIVE'], false),
    /** R1 ships without live analytics — keep false until Release 2 wiring. */
    analyticsEnabled: flag(process.env['NEXT_PUBLIC_FF_ANALYTICS'], false),
  };
}

export const featureFlags = resolveFeatureFlags();

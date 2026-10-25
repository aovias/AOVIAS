export const ANALYTICS_PROVIDER_IDS = [
  'mock',
  'google-analytics',
  'microsoft-clarity',
  'posthog',
  'mixpanel',
  'plausible',
  'opentelemetry',
  'internal',
] as const;

export type AnalyticsProviderId = (typeof ANALYTICS_PROVIDER_IDS)[number];

import { env } from '@/config/env';
import { featureFlags } from '@/config/feature-flags';

/**
 * Integration endpoint + mode configuration.
 * Release 2 fills base URLs; R1.5 keeps placeholders and mock mode.
 */

export type IntegrationServiceId =
  | 'identity'
  | 'commerce'
  | 'contact'
  | 'knowledge'
  | 'search'
  | 'analytics';

export type IntegrationServiceConfig = {
  id: IntegrationServiceId;
  /** Active adapter mode for this service. */
  mode: 'mock' | 'http';
  /** Future Release 2 base URL (unused by mocks). */
  baseUrl: string;
  enabled: boolean;
};

export type IntegrationConfig = {
  appEnv: typeof env.NEXT_PUBLIC_APP_ENV;
  siteUrl: string;
  services: Record<IntegrationServiceId, IntegrationServiceConfig>;
};

function service(
  id: IntegrationServiceId,
  baseUrlEnv: string | undefined,
  liveFlag: boolean,
  defaultPlaceholder: string,
): IntegrationServiceConfig {
  return {
    id,
    mode: liveFlag ? 'http' : 'mock',
    baseUrl: baseUrlEnv && baseUrlEnv.length > 0 ? baseUrlEnv : defaultPlaceholder,
    enabled: true,
  };
}

export function resolveIntegrationConfig(): IntegrationConfig {
  return {
    appEnv: env.NEXT_PUBLIC_APP_ENV,
    siteUrl: env.NEXT_PUBLIC_SITE_URL,
    services: {
      identity: service(
        'identity',
        process.env['NEXT_PUBLIC_IDENTITY_API_URL'],
        featureFlags.identityLive,
        'https://identity.example.aovias.local',
      ),
      commerce: service(
        'commerce',
        process.env['NEXT_PUBLIC_COMMERCE_API_URL'],
        featureFlags.commerceLive,
        'https://commerce.example.aovias.local',
      ),
      contact: service(
        'contact',
        process.env['NEXT_PUBLIC_CONTACT_API_URL'],
        featureFlags.contactLive,
        'https://contact.example.aovias.local',
      ),
      knowledge: service(
        'knowledge',
        process.env['NEXT_PUBLIC_KNOWLEDGE_API_URL'],
        featureFlags.knowledgeLive,
        'https://knowledge.example.aovias.local',
      ),
      search: service(
        'search',
        process.env['NEXT_PUBLIC_SEARCH_API_URL'],
        featureFlags.searchLive,
        'https://search.example.aovias.local',
      ),
      analytics: service(
        'analytics',
        process.env['NEXT_PUBLIC_ANALYTICS_API_URL'],
        featureFlags.analyticsEnabled,
        'https://analytics.example.aovias.local',
      ),
    },
  };
}

export const integrationConfig = resolveIntegrationConfig();

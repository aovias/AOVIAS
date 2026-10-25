import { env } from '@/config/env';
import { featureFlags } from '@/config/feature-flags';

export type HealthStatus = 'ok' | 'degraded' | 'error';

export type HealthSnapshot = {
  status: HealthStatus;
  service: 'aovias-website';
  version: string;
  environment: string;
  timestamp: string;
  uptimeSeconds: number;
};

export type ReadinessSnapshot = {
  ready: boolean;
  service: 'aovias-website';
  checks: {
    maintenance: boolean;
    configuration: boolean;
  };
  timestamp: string;
};

export type VersionSnapshot = {
  service: 'aovias-website';
  version: string;
  releaseId: string | null;
  buildId: string | null;
  artifactId: string | null;
  channel: string;
  specificationVersion: string;
  timestamp: string;
};

const startedAt = Date.now();

function packageVersion(): string {
  return (
    process.env['AOVIAS_RELEASE_VERSION'] ??
    process.env['npm_package_version'] ??
    '1.0.0'
  );
}

export function getHealthSnapshot(): HealthSnapshot {
  return {
    status: 'ok',
    service: 'aovias-website',
    version: packageVersion(),
    environment: env.NEXT_PUBLIC_APP_ENV,
    timestamp: new Date().toISOString(),
    uptimeSeconds: Math.floor((Date.now() - startedAt) / 1000),
  };
}

export function getReadinessSnapshot(): ReadinessSnapshot {
  const maintenance = process.env['MAINTENANCE_MODE'] === 'true';
  const configuration =
    typeof env.NEXT_PUBLIC_SITE_URL === 'string' &&
    env.NEXT_PUBLIC_SITE_URL.length > 0;

  return {
    ready: !maintenance && configuration,
    service: 'aovias-website',
    checks: {
      maintenance: !maintenance,
      configuration,
    },
    timestamp: new Date().toISOString(),
  };
}

/** Spec v1.0 version identity endpoint payload. */
export function getVersionSnapshot(): VersionSnapshot {
  return {
    service: 'aovias-website',
    version: packageVersion(),
    releaseId: process.env['AOVIAS_RELEASE_ID'] ?? null,
    buildId: process.env['AOVIAS_BUILD_ID'] ?? null,
    artifactId: process.env['AOVIAS_ARTIFACT_ID'] ?? null,
    channel: process.env['AOVIAS_RELEASE_CHANNEL'] ?? 'ga',
    specificationVersion: '1.0',
    timestamp: new Date().toISOString(),
  };
}

/** Provider-agnostic monitoring attributes for future exporters. */
export function getOpsAttributes(): Record<string, string | boolean | number> {
  return {
    'service.name': 'aovias-website',
    'deployment.environment': env.NEXT_PUBLIC_APP_ENV,
    'feature.analytics': featureFlags.analyticsEnabled,
    'feature.mock_sdks': featureFlags.useMockSdks,
  };
}

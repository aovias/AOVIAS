/**
 * Environment profiles for AOVIAS website integrations.
 * Concrete values come from `config/env`, `config/integration`, and feature flags.
 */

export type EnvironmentProfile = 'development' | 'preview' | 'production' | 'release2';

export const environmentProfiles: Record<
  EnvironmentProfile,
  { label: string; description: string }
> = {
  development: {
    label: 'Development',
    description: 'Local mock SDKs; no outbound integration calls.',
  },
  preview: {
    label: 'Preview',
    description: 'Staging-like previews; mocks by default until Release 2 flags enable HTTP.',
  },
  production: {
    label: 'Production',
    description: 'R1 public site — mock SDKs; Release 2 flips live flags per service.',
  },
  release2: {
    label: 'Release 2 (future)',
    description: 'Placeholder profile for live Identity, Commerce, Contact, Knowledge, Search.',
  },
};

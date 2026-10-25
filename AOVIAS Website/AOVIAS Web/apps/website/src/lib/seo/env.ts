/**
 * Environment helpers for SEO (site URL, indexing posture).
 */

export type AppEnvironment =
  | 'development'
  | 'preview'
  | 'staging'
  | 'production'
  | 'test';

export function getAppEnvironment(): AppEnvironment {
  const value =
    process.env['NEXT_PUBLIC_APP_ENV'] ?? process.env['NODE_ENV'] ?? 'development';
  if (value === 'production') return 'production';
  if (value === 'preview' || value === 'staging' || value === 'test') return value;
  return 'development';
}

/** True when search engines should index (production only by default). */
export function shouldAllowIndexing(): boolean {
  return getAppEnvironment() === 'production';
}

export function getRawSiteUrl(): string | undefined {
  const fromEnv = process.env['NEXT_PUBLIC_SITE_URL']?.trim();
  return fromEnv && fromEnv.length > 0 ? fromEnv.replace(/\/$/, '') : undefined;
}

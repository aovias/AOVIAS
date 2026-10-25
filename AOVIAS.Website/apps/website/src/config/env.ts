import { z } from 'zod';

const optionalUrl = z.union([z.string().url(), z.literal('')]).optional();

const envSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('development'),
  NEXT_PUBLIC_SITE_URL: z.string().url().default('http://localhost:3000'),
  NEXT_PUBLIC_APP_ENV: z
    .enum(['development', 'preview', 'staging', 'production'])
    .default('development'),
  /** Release 2 placeholders — unused by mock adapters. */
  NEXT_PUBLIC_IDENTITY_API_URL: optionalUrl,
  NEXT_PUBLIC_COMMERCE_API_URL: optionalUrl,
  NEXT_PUBLIC_CONTACT_API_URL: optionalUrl,
  NEXT_PUBLIC_KNOWLEDGE_API_URL: optionalUrl,
  NEXT_PUBLIC_SEARCH_API_URL: optionalUrl,
  NEXT_PUBLIC_ANALYTICS_API_URL: optionalUrl,
  /** Telemetry provider id placeholder (mock until Release 2). */
  NEXT_PUBLIC_ANALYTICS_PROVIDER: z.string().optional().default('mock'),
  /** Operations — when true, middleware serves maintenance page. */
  MAINTENANCE_MODE: z
    .enum(['true', 'false', ''])
    .optional()
    .default('false'),
});

export type AppEnv = z.infer<typeof envSchema>;

function readEnv(): AppEnv {
  const parsed = envSchema.safeParse({
    NODE_ENV: process.env['NODE_ENV'],
    NEXT_PUBLIC_SITE_URL: process.env['NEXT_PUBLIC_SITE_URL'],
    NEXT_PUBLIC_APP_ENV: process.env['NEXT_PUBLIC_APP_ENV'],
    NEXT_PUBLIC_IDENTITY_API_URL: process.env['NEXT_PUBLIC_IDENTITY_API_URL'],
    NEXT_PUBLIC_COMMERCE_API_URL: process.env['NEXT_PUBLIC_COMMERCE_API_URL'],
    NEXT_PUBLIC_CONTACT_API_URL: process.env['NEXT_PUBLIC_CONTACT_API_URL'],
    NEXT_PUBLIC_KNOWLEDGE_API_URL: process.env['NEXT_PUBLIC_KNOWLEDGE_API_URL'],
    NEXT_PUBLIC_SEARCH_API_URL: process.env['NEXT_PUBLIC_SEARCH_API_URL'],
    NEXT_PUBLIC_ANALYTICS_API_URL: process.env['NEXT_PUBLIC_ANALYTICS_API_URL'],
    NEXT_PUBLIC_ANALYTICS_PROVIDER: process.env['NEXT_PUBLIC_ANALYTICS_PROVIDER'],
    MAINTENANCE_MODE: process.env['MAINTENANCE_MODE'] ?? 'false',
  });

  if (!parsed.success) {
    const message = parsed.error.issues.map((issue) => issue.message).join('; ');
    throw new Error(`Invalid environment configuration: ${message}`);
  }

  return parsed.data;
}

export const env = readEnv();

import type { ReactNode } from 'react';

import { BrandAtmosphere, BrandMark } from '@/components/brand';
import { Paragraph, Stack, Text } from '@/components/primitives';
import { AppShell } from '@/layouts/app-shell';
import { siteConfig } from '@/config/site';

export type AuthenticationLayoutProps = {
  children: ReactNode;
};

/**
 * Authentication Layout — compact chrome + brand panel + form column.
 */
export function AuthenticationLayout({ children }: AuthenticationLayoutProps) {
  return (
    <AppShell
      variant="authentication"
      contentWidth="full"
      compactChrome
      showBreadcrumb={false}
      atmosphere="security"
    >
      <div className="grid min-h-[calc(100dvh-8rem)] w-full lg:grid-cols-2">
        <aside
          className="bg-muted/50 border-border relative hidden flex-col justify-between overflow-hidden border-e px-10 py-12 lg:flex"
          aria-label="AOVIAS Identity"
        >
          <BrandAtmosphere
            variant="trust-geometry"
            density="subtle"
            className="opacity-90"
          />

          <Stack gap={6} className="relative z-10 max-w-md">
            <div className="flex items-center gap-3">
              <BrandMark className="size-11" />
              <Text
                as="p"
                size="caption"
                weight="medium"
                tone="muted"
                className="tracking-wide uppercase"
              >
                AOVIAS Identity
              </Text>
            </div>
            <p className="text-foreground text-h2 font-semibold tracking-tight">
              One secure sign-in for every AOVIAS product
            </p>
            <Paragraph tone="muted">
              Sign in once to access the Public Website, Studio, Command, and
              future products — with the same professional identity experience.
            </Paragraph>
          </Stack>
          <Text as="p" size="caption" tone="muted" className="relative z-10">
            {siteConfig.name} · Trusted institutional technology
          </Text>
        </aside>

        <div className="bg-background flex w-full items-start justify-center px-4 py-10 sm:px-8 lg:py-16">
          <div className="w-full max-w-md">{children}</div>
        </div>
      </div>
    </AppShell>
  );
}

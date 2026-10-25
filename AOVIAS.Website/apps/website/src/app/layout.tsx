import type { ReactNode } from 'react';

import { SkipToContent } from '@/components/layout';
import { JsonLdScript } from '@/components/seo';
import {
  fontBodyClassName,
  fontVariableClassName,
} from '@/lib/performance';
import { buildDefaultSiteSchemas, createRootMetadata } from '@/lib/seo';
import { TelemetryBootstrap } from '@/providers/analytics';
import { AppProviders } from '@/providers/app-providers';
import '@/styles/globals.css';

/** Site-wide SEO defaults — pages use `createPageMetadata` for overrides. */
export const metadata = createRootMetadata();

type RootLayoutProps = {
  children: ReactNode;
};

/**
 * Root layout — HTML shell, fonts, metadata, global providers, a11y skip link.
 * Page chrome is owned by specialized layouts via AppShell composition.
 */
export default function RootLayout({ children }: RootLayoutProps) {
  return (
    <html
      lang="en"
      data-theme="dark"
      suppressHydrationWarning
      className={fontVariableClassName}
    >
      <body className={fontBodyClassName} suppressHydrationWarning>
        <JsonLdScript id="site-schema" data={buildDefaultSiteSchemas()} />
        <AppProviders>
          <SkipToContent />
          {children}
          <TelemetryBootstrap />
        </AppProviders>
      </body>
    </html>
  );
}

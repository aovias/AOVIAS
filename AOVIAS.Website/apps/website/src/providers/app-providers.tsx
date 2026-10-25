'use client';

import type { ReactNode } from 'react';

import { ErrorBoundary, NotificationContainer } from '@/components/layout';
import { ThemeProvider } from '@/providers/theme';
import { TelemetryProvider } from '@/telemetry';

export type AppProvidersProps = {
  children: ReactNode;
};

/**
 * Global client providers for the document shell.
 * MotionConfig is applied locally where Framer is used (V11 — keep framer off the root critical path).
 * TelemetryProvider injects the analytics SDK (mock / inactive in R1).
 */
export function AppProviders({ children }: AppProvidersProps) {
  return (
    <ThemeProvider defaultTheme="dark">
      <TelemetryProvider>
        <ErrorBoundary>
          {children}
          <NotificationContainer />
        </ErrorBoundary>
      </TelemetryProvider>
    </ThemeProvider>
  );
}

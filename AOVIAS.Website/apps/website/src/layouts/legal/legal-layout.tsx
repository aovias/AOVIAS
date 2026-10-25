import type { ReactNode } from 'react';

import { AppShell } from '@/layouts/app-shell';
import type { AvlfAtmosphereId } from '@/registries/avlf';

export type LegalLayoutProps = {
  children: ReactNode;
  atmosphere?: AvlfAtmosphereId;
};

/** Privacy, Terms, Cookies, Accessibility, Security — Trust & Governance. */
export function LegalLayout({
  children,
  atmosphere = 'minimal',
}: LegalLayoutProps) {
  return (
    <AppShell
      variant="legal"
      contentWidth="narrow"
      showBreadcrumb
      atmosphere={atmosphere}
      injectAtmosphere
      atmosphereDensity="subtle"
    >
      {children}
    </AppShell>
  );
}

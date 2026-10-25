import type { ReactNode } from 'react';

import { AppShell } from '@/layouts/app-shell';
import type { AvlfAtmosphereId } from '@/registries/avlf';

export type MarketingLayoutProps = {
  children: ReactNode;
  /** AVLS EBAS family for this marketing surface. */
  atmosphere?: AvlfAtmosphereId;
};

/** Home, Company, Industries, Solutions, Contact, Careers. */
export function MarketingLayout({
  children,
  atmosphere = 'enterprise',
}: MarketingLayoutProps) {
  return (
    <AppShell
      variant="marketing"
      contentWidth="full"
      showBreadcrumb={false}
      atmosphere={atmosphere}
    >
      {children}
    </AppShell>
  );
}

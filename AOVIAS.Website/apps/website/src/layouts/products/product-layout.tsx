import type { ReactNode } from 'react';

import { AppShell } from '@/layouts/app-shell';
import type { AvlfAtmosphereId } from '@/registries/avlf';

export type ProductLayoutProps = {
  children: ReactNode;
  atmosphere?: AvlfAtmosphereId;
};

/** Product overview, details, pricing, features, landings. */
export function ProductLayout({
  children,
  atmosphere = 'workflow',
}: ProductLayoutProps) {
  return (
    <AppShell
      variant="product"
      contentWidth="full"
      showBreadcrumb
      atmosphere={atmosphere}
    >
      {children}
    </AppShell>
  );
}

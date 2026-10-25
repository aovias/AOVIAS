import type { ReactNode } from 'react';

import { AppShell } from '@/layouts/app-shell';
import type { AvlfAtmosphereId } from '@/registries/avlf';

export type TechnologyLayoutProps = {
  children: ReactNode;
  atmosphere?: AvlfAtmosphereId;
};

/**
 * Technology section shell — breadcrumbs on; sidebar reserved for later sprints.
 */
export function TechnologyLayout({
  children,
  atmosphere = 'blueprint',
}: TechnologyLayoutProps) {
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

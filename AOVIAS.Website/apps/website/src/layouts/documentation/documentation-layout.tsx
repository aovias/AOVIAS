import type { ReactNode } from 'react';

import { AppShell } from '@/layouts/app-shell';
import type { AvlfAtmosphereId } from '@/registries/avlf';

export type DocumentationLayoutProps = {
  children: ReactNode;
  atmosphere?: AvlfAtmosphereId;
};

/** Documentation, guides, API docs, knowledge base. */
export function DocumentationLayout({
  children,
  atmosphere = 'knowledge',
}: DocumentationLayoutProps) {
  return (
    <AppShell
      variant="documentation"
      contentWidth="hero"
      showSidebar
      showBreadcrumb
      atmosphere={atmosphere}
      injectAtmosphere
      atmosphereDensity="subtle"
    >
      {children}
    </AppShell>
  );
}

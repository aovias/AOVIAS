import type { ReactNode } from 'react';

import { AppShell } from '@/layouts/app-shell';
import type { AvlfAtmosphereId } from '@/registries/avlf';

export type KnowledgeLayoutProps = {
  children: ReactNode;
  atmosphere?: AvlfAtmosphereId;
};

/**
 * Knowledge / editorial chrome — marketing shell with breadcrumbs for reading paths.
 * Documentation routes continue to use DocumentationLayout.
 */
export function KnowledgeLayout({
  children,
  atmosphere = 'knowledge',
}: KnowledgeLayoutProps) {
  return (
    <AppShell
      variant="marketing"
      contentWidth="full"
      showBreadcrumb
      atmosphere={atmosphere}
    >
      {children}
    </AppShell>
  );
}

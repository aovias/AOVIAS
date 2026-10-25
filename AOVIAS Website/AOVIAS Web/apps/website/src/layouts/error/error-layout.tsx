import type { ReactNode } from 'react';

import { AppShell } from '@/layouts/app-shell';

export type ErrorLayoutProps = {
  children: ReactNode;
};

/** 404, 500, unauthorized, maintenance. */
export function ErrorLayout({ children }: ErrorLayoutProps) {
  return (
    <AppShell
      variant="error"
      contentWidth="narrow"
      compactChrome
      showBreadcrumb={false}
      showFooter={false}
      atmosphere="minimal"
    >
      {children}
    </AppShell>
  );
}

import type { ReactNode } from 'react';

import { TechnologyLayout } from '@/layouts/technology/technology-layout';

/** Technology routes share the Technology shell (breadcrumbs on). */
export default function TechnologyRouteLayout({
  children,
}: {
  children: ReactNode;
}) {
  return <TechnologyLayout>{children}</TechnologyLayout>;
}

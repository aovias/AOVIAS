import type { ReactNode } from 'react';

import { DocumentationLayout } from '@/layouts/documentation/documentation-layout';

export default function ResourcesRouteLayout({ children }: { children: ReactNode }) {
  return <DocumentationLayout>{children}</DocumentationLayout>;
}

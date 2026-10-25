import type { ReactNode } from 'react';

import { LegalLayout } from '@/layouts/legal/legal-layout';

export default function LegalRouteLayout({ children }: { children: ReactNode }) {
  return <LegalLayout>{children}</LegalLayout>;
}

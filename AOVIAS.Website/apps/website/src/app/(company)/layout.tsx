import type { ReactNode } from 'react';

import { MarketingLayout } from '@/layouts/marketing/marketing-layout';

/** Company routes share the marketing shell with corporate atmosphere. */
export default function CompanyRouteLayout({ children }: { children: ReactNode }) {
  return <MarketingLayout atmosphere="corporate">{children}</MarketingLayout>;
}

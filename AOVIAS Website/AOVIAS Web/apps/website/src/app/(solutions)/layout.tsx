import type { ReactNode } from 'react';

import { MarketingLayout } from '@/layouts/marketing/marketing-layout';

/** Solutions / Industries routes share the marketing shell with corporate atmosphere. */
export default function SolutionsRouteLayout({
  children,
}: {
  children: ReactNode;
}) {
  return <MarketingLayout atmosphere="corporate">{children}</MarketingLayout>;
}

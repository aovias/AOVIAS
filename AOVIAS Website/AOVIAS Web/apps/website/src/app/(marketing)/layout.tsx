import type { ReactNode } from 'react';

import { MarketingLayout } from '@/layouts/marketing/marketing-layout';

export default function MarketingRouteLayout({ children }: { children: ReactNode }) {
  return <MarketingLayout>{children}</MarketingLayout>;
}

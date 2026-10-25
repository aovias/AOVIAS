import type { ReactNode } from 'react';

import { ProductLayout } from '@/layouts/products/product-layout';

export default function ProductsRouteLayout({ children }: { children: ReactNode }) {
  return <ProductLayout>{children}</ProductLayout>;
}

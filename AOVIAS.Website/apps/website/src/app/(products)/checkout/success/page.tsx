import {
  CheckoutSuccessPage,
  checkoutSeo,
  parseCheckoutSelection,
} from '@/features/checkout';
import { createPageMetadata } from '@/lib/seo';

const seo = checkoutSeo.success;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'product',
  noIndex: true,
  robots: { index: false, follow: false },
});

type SuccessRouteProps = {
  searchParams: Promise<{
    productId?: string;
    planId?: string;
    cycleId?: string;
    quantity?: string;
  }>;
};

export default async function CheckoutSuccessRoutePage({
  searchParams,
}: SuccessRouteProps) {
  const params = await searchParams;
  return <CheckoutSuccessPage selection={parseCheckoutSelection(params)} />;
}

import {
  CheckoutCartPage,
  checkoutSeo,
  parseCheckoutSelection,
} from '@/features/checkout';
import { createPageMetadata } from '@/lib/seo';

const seo = checkoutSeo.checkout;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'product',
  noIndex: true,
  robots: { index: false, follow: false },
});

type CheckoutRouteProps = {
  searchParams: Promise<{
    productId?: string;
    planId?: string;
    cycleId?: string;
    quantity?: string;
  }>;
};

/**
 * Checkout — selection + order summary (Product Layout).
 */
export default async function CheckoutRoutePage({
  searchParams,
}: CheckoutRouteProps) {
  const params = await searchParams;
  const selection = parseCheckoutSelection(params);
  return <CheckoutCartPage initial={selection} />;
}

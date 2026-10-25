import {
  CheckoutBillingPage,
  checkoutSeo,
  parseCheckoutSelection,
} from '@/features/checkout';
import { createPageMetadata } from '@/lib/seo';

const seo = checkoutSeo.billing;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'product',
  noIndex: true,
  robots: { index: false, follow: false },
});

type BillingRouteProps = {
  searchParams: Promise<{
    productId?: string;
    planId?: string;
    cycleId?: string;
    quantity?: string;
  }>;
};

export default async function CheckoutBillingRoutePage({
  searchParams,
}: BillingRouteProps) {
  const params = await searchParams;
  return <CheckoutBillingPage selection={parseCheckoutSelection(params)} />;
}

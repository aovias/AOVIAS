import {
  CheckoutConfirmPage,
  checkoutSeo,
  parseCheckoutSelection,
} from '@/features/checkout';
import { createPageMetadata } from '@/lib/seo';

const seo = checkoutSeo.confirm;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  type: 'product',
  noIndex: true,
  robots: { index: false, follow: false },
});

type ConfirmRouteProps = {
  searchParams: Promise<{
    productId?: string;
    planId?: string;
    cycleId?: string;
    quantity?: string;
  }>;
};

export default async function CheckoutConfirmRoutePage({
  searchParams,
}: ConfirmRouteProps) {
  const params = await searchParams;
  return <CheckoutConfirmPage selection={parseCheckoutSelection(params)} />;
}

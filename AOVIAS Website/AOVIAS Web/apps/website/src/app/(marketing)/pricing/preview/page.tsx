import { redirect } from 'next/navigation';

/**
 * Legacy pricing preview shell — redirect to production Pricing.
 */
export default function PricingLayoutPreviewPage() {
  redirect('/pricing');
}

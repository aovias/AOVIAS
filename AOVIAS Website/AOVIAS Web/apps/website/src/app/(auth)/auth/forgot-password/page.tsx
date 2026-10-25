import { ForgotPasswordPage, identitySeo } from '@/features/identity';
import { createPageMetadata } from '@/lib/seo';

const seo = identitySeo.forgotPassword;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  noIndex: true,
  robots: { index: false, follow: false },
});

/**
 * AOVIAS Identity — Forgot Password.
 */
export default function ForgotPasswordRoutePage() {
  return <ForgotPasswordPage />;
}

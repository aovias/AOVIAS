import { SignUpPage, identitySeo } from '@/features/identity';
import { createPageMetadata } from '@/lib/seo';

const seo = identitySeo.signUp;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  noIndex: true,
  robots: { index: false, follow: false },
});

/**
 * AOVIAS Identity — Sign Up.
 */
export default function SignUpRoutePage() {
  return <SignUpPage />;
}

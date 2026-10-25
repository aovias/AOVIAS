import { SignInPage, identitySeo } from '@/features/identity';
import { createPageMetadata } from '@/lib/seo';
import { resolveSafeRedirect } from '@/services/identity';

const seo = identitySeo.signIn;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  noIndex: true,
  robots: { index: false, follow: false },
});

type SignInRouteProps = {
  searchParams: Promise<{ redirect?: string; returnUrl?: string }>;
};

/**
 * AOVIAS Identity — Sign In.
 */
export default async function SignInRoutePage({
  searchParams,
}: SignInRouteProps) {
  const params = await searchParams;
  const redirectTo = resolveSafeRedirect(
    params.redirect ?? params.returnUrl ?? null,
  );

  return (
    <SignInPage {...(redirectTo !== '/' ? { redirectTo } : {})} />
  );
}

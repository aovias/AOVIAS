import { VerifyEmailPage, identitySeo } from '@/features/identity';
import { createPageMetadata } from '@/lib/seo';

const seo = identitySeo.verifyEmail;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  noIndex: true,
  robots: { index: false, follow: false },
});

type VerifyEmailRouteProps = {
  searchParams: Promise<{ token?: string }>;
};

/**
 * AOVIAS Identity — Email Verification (`?token=` from Identity email).
 */
export default async function VerifyEmailRoutePage({
  searchParams,
}: VerifyEmailRouteProps) {
  const params = await searchParams;
  return (
    <VerifyEmailPage {...(params.token ? { token: params.token } : {})} />
  );
}

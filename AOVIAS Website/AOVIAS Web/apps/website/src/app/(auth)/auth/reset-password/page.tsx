import { ResetPasswordPage, identitySeo } from '@/features/identity';
import { createPageMetadata } from '@/lib/seo';

const seo = identitySeo.resetPassword;

export const metadata = createPageMetadata({
  title: seo.title,
  description: seo.description,
  path: seo.path,
  noIndex: true,
  robots: { index: false, follow: false },
});

type ResetPasswordRouteProps = {
  searchParams: Promise<{ token?: string }>;
};

/**
 * AOVIAS Identity — Reset Password (`?token=` from Identity email).
 */
export default async function ResetPasswordRoutePage({
  searchParams,
}: ResetPasswordRouteProps) {
  const params = await searchParams;
  return (
    <ResetPasswordPage {...(params.token ? { token: params.token } : {})} />
  );
}

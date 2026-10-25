import type { ReactNode } from 'react';

import { AuthenticationLayout } from '@/layouts/authentication/authentication-layout';

export default function AuthRouteLayout({ children }: { children: ReactNode }) {
  return <AuthenticationLayout>{children}</AuthenticationLayout>;
}

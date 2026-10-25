import { redirect } from 'next/navigation';

/**
 * Legacy auth layout preview — redirect to Sign In.
 */
export default function AuthLayoutPreviewPage() {
  redirect('/auth/sign-in');
}

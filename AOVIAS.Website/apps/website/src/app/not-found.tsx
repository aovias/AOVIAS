import Link from 'next/link';

import { PageHeader, Section, Stack } from '@/components/layout';
import { ErrorLayout } from '@/layouts/error/error-layout';

export default function NotFound() {
  return (
    <ErrorLayout>
      <Section spacing="sm">
        <PageHeader
          eyebrow="404"
          title="Page not found"
          description="The requested resource does not exist. This uses the shared ErrorLayout shell."
        />
        <Stack gap={3}>
          <Link href="/" className="text-primary text-small font-medium underline-offset-4 hover:underline">
            Return home
          </Link>
        </Stack>
      </Section>
    </ErrorLayout>
  );
}

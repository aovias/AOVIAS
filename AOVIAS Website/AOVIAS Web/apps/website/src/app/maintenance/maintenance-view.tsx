import Link from 'next/link';

import { PageHeader, Section, Stack } from '@/components/layout';
import { ErrorLayout } from '@/layouts/error/error-layout';

export function MaintenanceView() {
  return (
    <ErrorLayout>
      <Section spacing="sm">
        <PageHeader
          eyebrow="Maintenance"
          title="We’ll be right back"
          description="AOVIAS is undergoing scheduled maintenance. Public services will resume shortly."
        />
        <Stack gap={3}>
          <p className="text-muted text-small">
            If you need urgent assistance, email admin@aovias.com.
          </p>
          <Link
            href="/"
            className="text-primary text-small font-medium underline-offset-4 hover:underline"
          >
            Return home
          </Link>
        </Stack>
      </Section>
    </ErrorLayout>
  );
}

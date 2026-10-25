'use client';

import Link from 'next/link';
import { useEffect } from 'react';

import { PageHeader, Section, Stack } from '@/components/layout';
import { ErrorLayout } from '@/layouts/error/error-layout';
import { getTelemetryClient } from '@/telemetry';

type ErrorPageProps = {
  error: Error & { digest?: string };
  reset: () => void;
};

/**
 * Segment error UI — replaces framework exception pages for recoverable route errors.
 */
export default function ErrorPage({ error, reset }: ErrorPageProps) {
  useEffect(() => {
    getTelemetryClient().trackError({
      message: error.message,
      name: error.name,
      fatal: false,
      attributes: {
        ...(error.digest ? { 'error.digest': error.digest } : {}),
      },
    });
  }, [error]);

  return (
    <ErrorLayout>
      <Section spacing="sm">
        <PageHeader
          eyebrow="500"
          title="Something went wrong"
          description="We could not complete this request. You can try again, or return to the homepage."
        />
        <Stack gap={3}>
          <button
            type="button"
            onClick={reset}
            className="text-primary text-small w-fit font-medium underline-offset-4 hover:underline"
          >
            Try again
          </button>
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

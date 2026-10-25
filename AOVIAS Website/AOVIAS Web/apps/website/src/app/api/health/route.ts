import { NextResponse } from 'next/server';

import { getHealthSnapshot } from '@/lib/ops/health';

/**
 * Liveness probe — process is up and can serve.
 * Used by Docker HEALTHCHECK and orchestrators.
 */
export function GET(): NextResponse {
  const snapshot = getHealthSnapshot();
  return NextResponse.json(snapshot, {
    status: snapshot.status === 'ok' ? 200 : 503,
    headers: {
      'Cache-Control': 'no-store',
    },
  });
}

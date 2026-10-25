import { NextResponse } from 'next/server';

import { getReadinessSnapshot } from '@/lib/ops/health';

/**
 * Readiness probe — safe to receive traffic.
 * Future: gate on dependency checks (Identity, CMS, etc.).
 */
export function GET(): NextResponse {
  const snapshot = getReadinessSnapshot();
  return NextResponse.json(snapshot, {
    status: snapshot.ready ? 200 : 503,
    headers: {
      'Cache-Control': 'no-store',
    },
  });
}

import { NextResponse } from 'next/server';

import { getHealthSnapshot } from '@/lib/ops/health';

/**
 * Spec-stable liveness alias — same semantics as /api/health.
 * AOVIAS Release Specification v1.0 health contract.
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

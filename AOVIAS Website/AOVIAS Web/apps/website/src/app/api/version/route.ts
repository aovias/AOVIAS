import { NextResponse } from 'next/server';

import { getVersionSnapshot } from '@/lib/ops/health';

/**
 * Release identity — AOVIAS Release Specification v1.0 /api/version.
 */
export function GET(): NextResponse {
  return NextResponse.json(getVersionSnapshot(), {
    status: 200,
    headers: {
      'Cache-Control': 'no-store',
    },
  });
}

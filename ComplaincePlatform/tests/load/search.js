/**
 * Phase 7 Sprint 6 — compliance search load script (k6).
 *
 * GET /api/v1/compliance/search?q=...
 * Requires TOKEN (ComplianceScope). Without TOKEN, script exits early with a warning VU.
 *
 * Env:
 *   BASE_URL  Gateway base (default http://localhost:5000)
 *   TOKEN     Bearer JWT (required for meaningful run)
 *   SEARCH_Q  query string (default nabh)
 */
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5000').replace(/\/$/, '');
const TOKEN = __ENV.TOKEN || '';
const SEARCH_Q = encodeURIComponent(__ENV.SEARCH_Q || 'nabh');

export const options = {
  scenarios: {
    search: {
      executor: 'constant-arrival-rate',
      rate: 8,
      timeUnit: '1s',
      duration: '1m',
      preAllocatedVUs: 8,
      maxVUs: 30,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.15'],
    http_req_duration: ['p(95)<4000'],
  },
};

export function setup() {
  if (!TOKEN) {
    console.warn(
      'TOKEN unset — search.js will only assert Gateway /health. Export TOKEN for authenticated search load.'
    );
  }
  return {};
}

export default function () {
  if (!TOKEN) {
    const health = http.get(`${BASE_URL}/health`, { tags: { name: 'gateway_health' } });
    check(health, { 'health is 200': (r) => r.status === 200 });
    sleep(0.5);
    return;
  }

  const res = http.get(`${BASE_URL}/api/v1/compliance/search?q=${SEARCH_Q}&page=1&pageSize=10`, {
    headers: {
      Accept: 'application/json',
      Authorization: `Bearer ${TOKEN}`,
    },
    tags: { name: 'compliance_search' },
  });

  check(res, {
    'search status is 200 or 403': (r) => r.status === 200 || r.status === 403,
    'search not 5xx': (r) => r.status < 500,
  });

  sleep(0.25);
}

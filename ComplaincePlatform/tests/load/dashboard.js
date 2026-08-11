/**
 * Phase 7 Sprint 6 — dashboard / role-home load script (k6).
 *
 * Always hits Gateway /health. When TOKEN is set, also hits:
 *   GET /api/v1/dashboard/summary
 *   GET /api/v1/role-experience/home
 *
 * Env:
 *   BASE_URL  Gateway base (default http://localhost:5000)
 *   TOKEN     Bearer JWT (optional; authenticated paths skipped when unset)
 */
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5000').replace(/\/$/, '');
const TOKEN = __ENV.TOKEN || '';

export const options = {
  scenarios: {
    dashboard: {
      executor: 'constant-arrival-rate',
      rate: 10,
      timeUnit: '1s',
      duration: '1m',
      preAllocatedVUs: 10,
      maxVUs: 40,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.10'],
    http_req_duration: ['p(95)<3000'],
    checks: ['rate>0.90'],
  },
};

function authHeaders() {
  const headers = { Accept: 'application/json' };
  if (TOKEN) {
    headers.Authorization = `Bearer ${TOKEN}`;
  }
  return headers;
}

export default function () {
  const health = http.get(`${BASE_URL}/health`, { tags: { name: 'gateway_health' } });
  check(health, {
    'health is 200': (r) => r.status === 200,
  });

  if (TOKEN) {
    const summary = http.get(`${BASE_URL}/api/v1/dashboard/summary`, {
      headers: authHeaders(),
      tags: { name: 'dashboard_summary' },
    });
    check(summary, {
      'dashboard summary reachable (2xx/4xx)': (r) => r.status >= 200 && r.status < 500,
      'dashboard summary not 5xx': (r) => r.status < 500,
    });

    const home = http.get(`${BASE_URL}/api/v1/role-experience/home`, {
      headers: authHeaders(),
      tags: { name: 'role_experience_home' },
    });
    check(home, {
      'role-home reachable (2xx/4xx)': (r) => r.status >= 200 && r.status < 500,
      'role-home not 5xx': (r) => r.status < 500,
    });
  }

  sleep(0.3);
}

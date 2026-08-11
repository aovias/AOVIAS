/**
 * Phase 7 Sprint 6 — soak (lower rate, longer duration).
 *
 * Rotates Gateway /health and optional authenticated dashboard/search paths.
 *
 * Env:
 *   BASE_URL       Gateway base (default http://localhost:5000)
 *   TOKEN          Bearer JWT (optional)
 *   SOAK_DURATION  k6 duration string (default 10m)
 *   SOAK_RATE      arrivals per second (default 2)
 */
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5000').replace(/\/$/, '');
const TOKEN = __ENV.TOKEN || '';
const SOAK_DURATION = __ENV.SOAK_DURATION || '10m';
const SOAK_RATE = Number(__ENV.SOAK_RATE || 2);

export const options = {
  scenarios: {
    soak: {
      executor: 'constant-arrival-rate',
      rate: SOAK_RATE,
      timeUnit: '1s',
      duration: SOAK_DURATION,
      preAllocatedVUs: Math.max(2, SOAK_RATE),
      maxVUs: Math.max(10, SOAK_RATE * 5),
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.10'],
    http_req_duration: ['p(95)<5000'],
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
  check(health, { 'health is 200': (r) => r.status === 200 });

  if (TOKEN) {
    const path =
      Math.random() < 0.5
        ? '/api/v1/dashboard/summary'
        : '/api/v1/compliance/search?q=soak&page=1&pageSize=5';
    const res = http.get(`${BASE_URL}${path}`, {
      headers: authHeaders(),
      tags: { name: 'soak_authenticated' },
    });
    check(res, {
      'authenticated path not 5xx': (r) => r.status < 500,
    });
  }

  sleep(1);
}

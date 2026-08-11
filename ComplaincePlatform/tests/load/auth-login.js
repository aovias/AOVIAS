/**
 * Phase 7 Sprint 6 — auth / smoke load script (k6).
 *
 * Prefer POST /api/v1/identity/login when LOGIN_EMAIL + LOGIN_PASSWORD are set.
 * Otherwise smoke GET /health (login is MFA / org-scoped and often unsuitable for CI).
 *
 * Env:
 *   BASE_URL     Gateway base (default http://localhost:5000)
 *   LOGIN_EMAIL  optional
 *   LOGIN_PASSWORD optional
 */
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = (__ENV.BASE_URL || 'http://localhost:5000').replace(/\/$/, '');
const LOGIN_EMAIL = __ENV.LOGIN_EMAIL || '';
const LOGIN_PASSWORD = __ENV.LOGIN_PASSWORD || '';
const useLogin = LOGIN_EMAIL.length > 0 && LOGIN_PASSWORD.length > 0;

export const options = {
  scenarios: {
    auth_smoke: {
      executor: 'constant-arrival-rate',
      rate: 5,
      timeUnit: '1s',
      duration: '30s',
      preAllocatedVUs: 5,
      maxVUs: 20,
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
    http_req_duration: ['p(95)<2000'],
  },
};

export default function () {
  if (useLogin) {
    const res = http.post(
      `${BASE_URL}/api/v1/identity/login`,
      JSON.stringify({ email: LOGIN_EMAIL, password: LOGIN_PASSWORD }),
      {
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        tags: { name: 'identity_login' },
      }
    );
    check(res, {
      'login status is 200 or 401/403 (reachable)': (r) =>
        r.status === 200 || r.status === 401 || r.status === 403 || r.status === 400,
    });
  } else {
    const res = http.get(`${BASE_URL}/health`, {
      tags: { name: 'gateway_health' },
    });
    check(res, {
      'health status is 200': (r) => r.status === 200,
    });
  }
  sleep(0.2);
}

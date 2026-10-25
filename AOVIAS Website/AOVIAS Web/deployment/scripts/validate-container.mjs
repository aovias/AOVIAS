#!/usr/bin/env node
/**
 * Post-deploy validation — health, home, robots, sitemap.
 */

const base = process.env['SITE_URL'] ?? 'http://127.0.0.1:3000';

const checks = [
  { name: 'health', path: '/api/health', expectStatus: 200, expectIncludes: '"status":"ok"' },
  { name: 'ready', path: '/api/ready', expectStatus: 200, expectIncludes: '"ready":true' },
  { name: 'home', path: '/', expectStatus: 200, expectIncludes: 'AOVIAS' },
  { name: 'robots', path: '/robots.txt', expectStatus: 200 },
  { name: 'sitemap', path: '/sitemap.xml', expectStatus: 200 },
];

let failed = false;

for (const check of checks) {
  const url = `${base}${check.path}`;
  try {
    const response = await fetch(url);
    const text = await response.text();
    const statusOk = response.status === check.expectStatus;
    const bodyOk = check.expectIncludes
      ? text.includes(check.expectIncludes) ||
        text.replace(/\s+/g, '').includes(check.expectIncludes.replace(/\s+/g, ''))
      : true;

    if (!statusOk || !bodyOk) {
      console.error(`✗ ${check.name}: status=${response.status} url=${url}`);
      failed = true;
    } else {
      console.log(`✓ ${check.name}`);
    }
  } catch (error) {
    console.error(`✗ ${check.name}: ${error instanceof Error ? error.message : error}`);
    failed = true;
  }
}

process.exit(failed ? 1 : 0);

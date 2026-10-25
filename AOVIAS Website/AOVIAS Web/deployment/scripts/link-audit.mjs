#!/usr/bin/env node
/**
 * R1.9 broken-link audit against a running origin (default localhost:3000).
 */

const base = (process.env['SITE_URL'] ?? 'http://127.0.0.1:3000').replace(
  /\/$/,
  '',
);

const seeds = [
  '/',
  '/company',
  '/company/contact',
  '/company/careers',
  '/products',
  '/products/command',
  '/products/studio',
  '/pricing',
  '/technology',
  '/technology/platform',
  '/technology/ai',
  '/technology/engineering',
  '/technology/trust',
  '/technology/integrations',
  '/solutions',
  '/solutions/healthcare',
  '/solutions/government',
  '/solutions/education',
  '/solutions/enterprise',
  '/resources',
  '/resources/docs',
  '/resources/blog',
  '/legal',
  '/legal/privacy',
  '/legal/terms',
  '/robots.txt',
  '/sitemap.xml',
  '/api/health',
];

const internalHref =
  /href=["'](\/[^"'#?]+(?:\?[^"']*)?(?:#[^"']*)?)["']/g;

/** @type {Map<string, Set<string>>} */
const failures = new Map();
/** @type {Set<string>} */
const checked = new Set();

async function check(path, from) {
  const url = path.startsWith('http') ? path : `${base}${path.split('#')[0]}`;
  const key = url;
  if (checked.has(key)) return;
  checked.add(key);

  try {
    const response = await fetch(url, { redirect: 'manual' });
    if (response.status >= 400) {
      if (!failures.has(url)) failures.set(url, new Set());
      failures.get(url).add(from);
      return;
    }
    // Follow one redirect only for audit visibility
    if (response.status >= 300 && response.status < 400) {
      const location = response.headers.get('location');
      if (location?.startsWith('/')) {
        await check(location, `${from} → redirect`);
      }
    }
  } catch (error) {
    if (!failures.has(url)) failures.set(url, new Set());
    failures.get(url).add(`${from} (${error instanceof Error ? error.message : error})`);
  }
}

async function crawl(path) {
  const url = `${base}${path}`;
  const response = await fetch(url);
  if (!response.ok) {
    if (!failures.has(url)) failures.set(url, new Set());
    failures.get(url).add('seed');
    return;
  }
  const html = await response.text();
  const hrefs = new Set();
  for (const match of html.matchAll(internalHref)) {
    const href = match[1];
    if (
      href.startsWith('/dev') ||
      href.startsWith('/internal') ||
      href.includes('/preview')
    ) {
      continue;
    }
    hrefs.add(href.split('#')[0] || '/');
  }
  for (const href of hrefs) {
    await check(href, path);
  }
}

console.log(`Link audit against ${base}`);
for (const seed of seeds) {
  await check(seed, 'seed');
  if (!seed.includes('.') && !seed.startsWith('/api')) {
    await crawl(seed);
  }
}

if (failures.size > 0) {
  console.error(`\n✗ ${failures.size} failing URL(s):`);
  for (const [url, sources] of failures) {
    console.error(`  ${url}`);
    for (const source of sources) console.error(`    from: ${source}`);
  }
  process.exit(1);
}

console.log(`✓ Checked ${checked.size} URLs — no broken internal links.`);

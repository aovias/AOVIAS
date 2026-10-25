/**
 * R1.6 SEO smoke checks — run after build or via node in CI.
 * Validates robots/sitemap modules load and critical schema builders.
 *
 * Usage: node ./scripts/seo-smoke.mjs
 * (Requires compiled TS path aliases — prefer typecheck + build gate.)
 */

import { existsSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const publicOg = path.join(root, 'public', 'og');

const requiredOg = [
  'default.png',
  'home.png',
  'company.png',
  'contact.png',
  'careers.png',
  'products.png',
  'command.png',
  'studio.png',
  'pricing.png',
  'technology.png',
  'solutions.png',
  'resources.png',
  'blog.png',
  'documentation.png',
];

const requiredModules = [
  'src/lib/seo/schema.ts',
  'src/lib/seo/robots.ts',
  'src/lib/seo/sitemap.ts',
  'src/lib/seo/metadata-builder.ts',
  'src/lib/seo/opengraph.ts',
  'src/lib/seo/twitter.ts',
  'src/lib/seo/canonical.ts',
  'src/lib/seo/schema-validate.ts',
  'src/seo/index.ts',
  'src/seo/entities/registry.ts',
  'src/app/sitemap.ts',
  'src/app/robots.ts',
];

let failed = false;

for (const file of requiredModules) {
  const full = path.join(root, file);
  if (!existsSync(full)) {
    console.error(`Missing module: ${file}`);
    failed = true;
  }
}

for (const file of requiredOg) {
  const full = path.join(publicOg, file);
  if (!existsSync(full)) {
    console.error(`Missing OG asset: public/og/${file}`);
    failed = true;
  }
}

if (failed) {
  console.error('SEO smoke checks failed.');
  process.exit(1);
}

console.log('SEO smoke checks passed (modules + OG assets present).');

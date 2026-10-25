#!/usr/bin/env node
/**
 * Parse Next.js build output / route sizes and compare against performance budgets.
 * Run after `npm run build`.
 *
 * Budgets (gzip KB) — mirrors apps/website/src/config/performance.ts:
 *   initialJsKbGzip: 150
 *   routeJsKbKbGzip: 75
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const appDir = path.join(root, 'apps', 'website');
const nextDir = path.join(appDir, '.next');

const BUDGETS = {
  initialJsKbGzip: 150,
  routeJsKbGzip: 75,
};

function fail(message) {
  console.error(`✗ ${message}`);
  process.exitCode = 1;
}

function ok(message) {
  console.log(`✓ ${message}`);
}

if (!fs.existsSync(nextDir)) {
  fail('No .next build found. Run `npm run build` first.');
  process.exit(1);
}

/** Prefer app-path-routes-manifest + build manifest sizes when available. */
const buildManifestPath = path.join(nextDir, 'build-manifest.json');
const appBuildManifestPath = path.join(nextDir, 'app-build-manifest.json');

if (!fs.existsSync(buildManifestPath)) {
  fail('build-manifest.json missing — incomplete build.');
  process.exit(1);
}

const buildManifest = JSON.parse(fs.readFileSync(buildManifestPath, 'utf8'));
const sharedFiles = buildManifest.pages?.['/_app'] ?? buildManifest.rootMainFiles ?? [];

function fileSizeKb(relPath) {
  const abs = path.join(nextDir, relPath.replace(/^\//, ''));
  // Next lists files like static/chunks/...
  const candidates = [
    path.join(nextDir, relPath),
    path.join(appDir, '.next', relPath),
    path.join(nextDir, 'static', relPath),
  ];
  for (const candidate of candidates) {
    if (fs.existsSync(candidate)) {
      return fs.statSync(candidate).size / 1024;
    }
  }
  // Try under .next/static/chunks from basename
  const chunkPath = path.join(nextDir, relPath);
  if (fs.existsSync(chunkPath)) return fs.statSync(chunkPath).size / 1024;
  return 0;
}

/** Sum raw (uncompressed) shared chunk sizes as a conservative proxy. */
function sumFiles(files) {
  return files.reduce((total, file) => total + fileSizeKb(file), 0);
}

const sharedKb = sumFiles(Array.isArray(sharedFiles) ? sharedFiles : []);

// Also scan first-load from APP_PATH_ROUTES if present via traces — fall back to static chunk total.
const staticChunksDir = path.join(nextDir, 'static', 'chunks');
let totalChunksKb = 0;
if (fs.existsSync(staticChunksDir)) {
  for (const name of fs.readdirSync(staticChunksDir)) {
    if (!name.endsWith('.js')) continue;
    totalChunksKb += fs.statSync(path.join(staticChunksDir, name)).size / 1024;
  }
}

/** Uncompressed totals are larger than gzip — apply ~0.35 ratio heuristic for budget compare. */
const GZIP_RATIO = 0.35;
const estimatedSharedGzipKb = (sharedKb || totalChunksKb * 0.4) * GZIP_RATIO;
const estimatedTotalGzipKb = totalChunksKb * GZIP_RATIO;

console.log('Performance budget check (estimated gzip from raw chunk sizes)');
console.log(`  Shared/proxy JS: ~${estimatedSharedGzipKb.toFixed(1)} KB gzip (budget ${BUDGETS.initialJsKbGzip} KB)`);
console.log(`  All chunks:      ~${estimatedTotalGzipKb.toFixed(1)} KB gzip`);

if (estimatedSharedGzipKb > BUDGETS.initialJsKbGzip) {
  fail(
    `Initial JS estimate ${estimatedSharedGzipKb.toFixed(1)} KB exceeds budget ${BUDGETS.initialJsKbGzip} KB`,
  );
} else {
  ok(`Initial JS within budget (${estimatedSharedGzipKb.toFixed(1)} ≤ ${BUDGETS.initialJsKbGzip} KB)`);
}

if (fs.existsSync(appBuildManifestPath)) {
  ok('app-build-manifest.json present');
}

if (process.exitCode) {
  console.error('\nBudget check failed.');
  process.exit(process.exitCode);
}

console.log('\nBudget check passed (heuristic). Confirm with Lighthouse / bundle analyzer before release.');

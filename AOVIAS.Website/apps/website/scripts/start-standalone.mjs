#!/usr/bin/env node
/**
 * Production local start for `output: 'standalone'`.
 * `next start` does not serve CSS/JS correctly with standalone — mirror Docker runner.
 */

import { cpSync, existsSync } from 'node:fs';
import { spawn } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const websiteRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const standaloneRoot = path.join(websiteRoot, '.next', 'standalone');
const serverJs = path.join(standaloneRoot, 'apps', 'website', 'server.js');
const staticSrc = path.join(websiteRoot, '.next', 'static');
const staticDest = path.join(
  standaloneRoot,
  'apps',
  'website',
  '.next',
  'static',
);
const publicSrc = path.join(websiteRoot, 'public');
const publicDest = path.join(standaloneRoot, 'apps', 'website', 'public');

if (!existsSync(serverJs)) {
  console.error(
    'Standalone server missing. Run: npm run build --workspace=@aovias/website',
  );
  process.exit(1);
}

if (!existsSync(staticSrc)) {
  console.error('Missing .next/static — rebuild the website workspace.');
  process.exit(1);
}

cpSync(staticSrc, staticDest, { recursive: true });
if (existsSync(publicSrc)) {
  cpSync(publicSrc, publicDest, { recursive: true });
}

const port = process.env['PORT'] ?? '3000';
const child = spawn(process.execPath, ['apps/website/server.js'], {
  cwd: standaloneRoot,
  env: {
    ...process.env,
    PORT: port,
    HOSTNAME: process.env['HOSTNAME'] ?? '0.0.0.0',
    NODE_ENV: 'production',
  },
  stdio: 'inherit',
});

child.on('exit', (code, signal) => {
  if (signal) process.kill(process.pid, signal);
  process.exit(code ?? 1);
});

#!/usr/bin/env node
/**
 * Bundle analysis entry — sets ANALYZE=true and builds the website workspace.
 * Usage: npm run analyze
 */
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');

console.log('Building @aovias/website with bundle analyzer (ANALYZE=true)…');

const result = spawnSync(
  'npm',
  ['run', 'build', '--workspace=@aovias/website'],
  {
    cwd: root,
    stdio: 'inherit',
    env: { ...process.env, ANALYZE: 'true' },
    shell: true,
  },
);

process.exit(result.status ?? 1);

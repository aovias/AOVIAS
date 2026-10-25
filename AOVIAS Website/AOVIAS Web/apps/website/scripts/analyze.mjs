#!/usr/bin/env node
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const cwd = path.dirname(path.dirname(fileURLToPath(import.meta.url)));

const result = spawnSync('npx', ['next', 'build'], {
  cwd,
  stdio: 'inherit',
  env: { ...process.env, ANALYZE: 'true' },
  shell: true,
});

process.exit(result.status ?? 1);

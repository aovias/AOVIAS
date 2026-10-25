#!/usr/bin/env node
/**
 * One-command local production deployment.
 * Builds the runner image and starts compose with health validation.
 */

import { spawnSync } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const composeFile = path.join(
  root,
  'deployment',
  'compose',
  'docker-compose.prod.yml',
);

function run(command, args) {
  const result = spawnSync(command, args, {
    cwd: root,
    stdio: 'inherit',
    env: process.env,
  });
  if (result.error) {
    console.error(result.error);
    process.exit(1);
  }
  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}

const hostPort = process.env['HOST_PORT'] ?? '3000';

async function probeHealth() {
  try {
    const response = await fetch(`http://127.0.0.1:${hostPort}/api/health`);
    if (!response.ok) return null;
    return await response.json();
  } catch {
    return null;
  }
}

async function main() {
  console.log('→ Building and starting AOVIAS website (production image)…');
  if (hostPort !== '3000') {
    console.log(`→ Using HOST_PORT=${hostPort}`);
  }
  run('docker', [
    'compose',
    '-f',
    composeFile,
    'up',
    '--build',
    '-d',
    '--remove-orphans',
  ]);

  console.log('→ Waiting for health…');
  const maxAttempts = 40;
  for (let i = 1; i <= maxAttempts; i += 1) {
    const body = await probeHealth();
    if (body?.status === 'ok') {
      console.log(`✓ Website healthy at http://localhost:${hostPort}`);
      console.log(JSON.stringify(body));
      return;
    }
    await delay(2000);
  }

  console.error(
    `✗ Health check failed after waiting (port ${hostPort}). If bind failed, free the port or set HOST_PORT.`,
  );
  process.exitCode = 1;
}

await main();

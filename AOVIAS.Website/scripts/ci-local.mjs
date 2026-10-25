import { spawnSync } from 'node:child_process';

const steps = [
  ['npm', ['ci']],
  ['npm', ['run', 'typecheck']],
  ['npm', ['run', 'lint']],
  ['npm', ['run', 'build']],
];

for (const [cmd, args] of steps) {
  console.log(`\n==> ${cmd} ${args.join(' ')}\n`);
  const result = spawnSync(cmd, args, {
    stdio: 'inherit',
    shell: true,
    env: {
      ...process.env,
      NEXT_TELEMETRY_DISABLED: '1',
      NEXT_PUBLIC_SITE_URL: process.env.NEXT_PUBLIC_SITE_URL ?? 'https://aovias.com',
      NEXT_PUBLIC_APP_ENV: process.env.NEXT_PUBLIC_APP_ENV ?? 'production',
    },
  });
  if (result.status !== 0) {
    process.exit(result.status ?? 1);
  }
}

console.log('\nCI local verification passed.\n');

# AOVIAS Web

Production foundation for the AOVIAS Public Website and future commercial web platform.

This repository follows the [Website Engineering Constitution](./docs/specifications/README.md).

## Stack

- Next.js 15 (App Router)
- React 19
- TypeScript (strict)
- Tailwind CSS v4
- shadcn/ui (configured; components deferred)
- Framer Motion (dependency only in Sprint 1.1)
- ESLint, Prettier, Husky, Commitlint

## Monorepo layout

```text
apps/website          Next.js application
packages/*            Shared TypeScript/ESLint configs + shared packages
deployment/           Production Docker, compose, env, nginx, CI, ops scripts
docs/                 Architecture, decisions, specifications, deployment runbooks
docker/               Compatibility shims → deployment/
scripts/              Repo automation scripts
tooling/              Shared tooling assets
```

## Commands

```bash
npm install
npm run dev
npm run lint
npm run typecheck
npm run build
```

## Production deploy (one command)

```bash
npm run deploy:local
```

Starts the production Docker image on http://localhost:3000. See `deployment/docs/` and `docs/development/r1.8-*.md`.

## Docker (development)

```bash
docker compose -f deployment/compose/docker-compose.yml up --build
```

## Design system

Shared tokens and themes live in `@aovias/design-system` (`packages/design-system`).

```bash
import '@aovias/design-system/css';
import { ThemeProvider, tokens } from '@aovias/design-system';
```

## Sprint status

- Sprint 1.1 — Project Setup ✅  
- Sprint 1.2 — Theme System ✅  
- Sprint 1.3 — Layout System ✅  
- Sprint 1.4 — not started

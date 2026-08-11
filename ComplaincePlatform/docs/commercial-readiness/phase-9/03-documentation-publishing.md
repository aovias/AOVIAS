# 03 — Documentation Publishing

**Phase:** 9.2  
**Sprint:** 2  
**Date:** 2026-07-26

## Published set

Location: `docs/commercial-readiness/phase-9/published/`

| Guide | File |
|-------|------|
| Administrator | `administrator-guide.md` |
| Deployment | `deployment-guide.md` |
| User | `user-guide.md` |
| Hospital | `hospital-guide.md` |
| Quality manual (lite) | `quality-manual-lite.md` |
| AI | `ai-guide.md` |
| Architecture | `architecture-guide.md` |
| Troubleshooting | `troubleshooting-guide.md` |
| API index | `api-documentation.md` |

## Publishing process

1. Keep guides concise and accurate to v1.0.0 entitlements and Gateway patterns.
2. Link from `commercial-launch/website/docs.html`.
3. For customer portals, copy or render this folder to the docs host; do not invent parallel conflicting manuals.
4. Version stamp: align wording with `VERSION` / `RELEASE_NOTES_v1.0.0.md` on each GA cut.

## What is intentionally out of scope

- Full OpenAPI dump checked into git (discover via Swagger)
- Payment/billing user docs (deferred)
- Customer-specific SOP text (hospitals own controlled manuals)

## Exit

Published guide pack complete and indexed for commercial website and CS handoff.

# 01 — Release Management (v1.0.0)

**Phase:** 9.1  
**Sprint:** 1  
**Date:** 2026-07-26

## Version freeze

| Item | Value |
|------|-------|
| `VERSION` | `1.0.0` |
| Git tag (process) | `v1.0.0` |
| Release branch (process) | `release/1.0.0` |
| Notes | [`RELEASE_NOTES_v1.0.0.md`](../../../RELEASE_NOTES_v1.0.0.md) |
| Changelog | [`CHANGELOG.md`](../../../CHANGELOG.md) |

## RC approval checklist

| Gate | Owner | Status |
|------|-------|--------|
| Migration validation (`scripts/ops/upgrade.ps1` dry-run) | Engineering | Ready |
| Security headers scan | Engineering | Phase 7 script |
| License activate smoke | Engineering | Phase 6 APIs |
| Demo seed enrichment | CS / Eng | DemoDataSeeder |
| Claims vs product review | Product | Phase 9 Sprint 6 |
| Go / No-Go | Launch Director | Sprint 6 report |

## Packaging

1. `.\scripts\build-praman-aovpkg.ps1 -Version 1.0.0`
2. `.\scripts\release\generate-sbom.ps1`
3. `.\scripts\release\generate-checksums.ps1`
4. Publish via AOVIAS Package Builder / Deployment Center

## Docker tagging

`aovias/command-{service}:1.0.0` (gateway, identity, organization, compliance, …)

## Exit

Release notes, VERSION, SBOM/checksum scripts, and manifest schema ready for official `v1.0.0` tag.

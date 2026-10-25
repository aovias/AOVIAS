# Customer Implementation Checklist

**Product:** AOVIAS Command v1.0.0  
**Audience:** Implementation lead, hospital Quality Manager, AOVIAS CS  
**Related:** [deployment-validation.md](deployment-validation.md), [initial-configuration.md](initial-configuration.md), [success-milestones.md](success-milestones.md)

Use this checklist for every new pilot or production hospital. Check items only when evidence exists (ticket, screenshot, API response, or signed acceptance).

## Phase A — Premises & access

| # | Item | Owner | Done |
|---|------|-------|------|
| A1 | Customer legal entity / organization ID confirmed | CS | ☐ |
| A2 | Environments agreed (pilot / prod); hostnames documented | Eng / Ops | ☐ |
| A3 | Admin contacts (primary + escalation) in CS CRM | CS | ☐ |
| A4 | Network allowlists / TLS certs ready | Customer IT | ☐ |
| A5 | Backup target (disk / blob) and retention agreed | Ops | ☐ |

## Phase B — Deploy & validate

| # | Item | Owner | Done |
|---|------|-------|------|
| B1 | Fresh install via setup wizard (`/setup`) or compose | Ops | ☐ |
| B2 | `scripts/ops/Validate-Environment.ps1` passed | Ops | ☐ |
| B3 | `scripts/ops/validate-production.ps1 -RunAutomated` reviewed | Ops | ☐ |
| B4 | License issued (`POST /api/v1/commercial/provision` or `/licenses/issue`) | CS / Ops | ☐ |
| B5 | License activated (online or offline) — see [../licensing/activation-guide.md](../licensing/activation-guide.md) | Admin | ☐ |
| B6 | Heartbeat confirmed from deployed host | Ops | ☐ |

## Phase C — Configure hospital

| # | Item | Owner | Done |
|---|------|-------|------|
| C1 | Organization, hospitals, departments, OU hierarchy | Admin | ☐ |
| C2 | Framework catalog activated (NABH / agreed standards) | QM | ☐ |
| C3 | OE ownership matrix seeded for in-scope departments | QM | ☐ |
| C4 | Journey enrolled; first evidence upload succeeds | QM | ☐ |
| C5 | Role homes verified for Owner / QM / Dept Head | CS | ☐ |

## Phase D — People & training

| # | Item | Owner | Done |
|---|------|-------|------|
| D1 | SUPER_ADMIN / ORG_ADMIN accounts provisioned | Admin | ☐ |
| D2 | Seat assignments within MaxSeats | Admin | ☐ |
| D3 | Training schedule completed (see [training-schedule.md](training-schedule.md)) | CS | ☐ |
| D4 | Support channel + diagnostics process explained | Support | ☐ |

## Phase E — Go-live gate

| # | Item | Owner | Done |
|---|------|-------|------|
| E1 | Success milestone M1–M3 met | CS + QM | ☐ |
| E2 | Backup smoke restore drill documented | Ops | ☐ |
| E3 | Customer signs pilot acceptance (or production go-live) | CS | ☐ |
| E4 | Health review scheduled (30-day) | CS | ☐ |

## Exit

Hospital can run accreditation workflows in AOVIAS without spreadsheets for the agreed scope, with license Active and support path known.

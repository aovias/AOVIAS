# Audit Routes Deep Check — Missing Screens

**Date:** 2026-07-29  
**Question:** Are there more missing Audit screens (esp. Internal Audit)?

## Verdict

**Internal Audit was never a dedicated deleted page.** Across git history (`8e57d05`, `8b9d27b`), QA inventory §6.1, and NAVIGATION-MODEL, **“Internal Audit” is only a nav label for `/app/audit` (the Audit Dashboard).**

What felt “missing” was **naming/discoverability**: the hierarchical sidebar child was titled **Overview**, while the header still said **Internal Audit**.

### Fix applied
- Sidebar: **Overview → Internal Audit** (`/app/audit`)
- Dashboard title: **Internal Audit**
- Aliases: `/app/audit/internal` → dashboard; `/app/audit/final` → reports
- Sidebar reports label: **Final Audit Reports**
- Dashboard quick link: **Assessment Workspace**

---

## Current `/app/audit/*` routes

| Route | Page | Notes |
|-------|------|-------|
| `/app/audit` | Internal Audit dashboard | Was “Audit Dashboard” |
| `/app/audit/internal` | → `/app/audit` | Alias |
| `/app/audit/findings` | Findings Workbench | |
| `/app/audit/findings/:id` | Finding Detail | |
| `/app/audit/corrective-actions` | DCC / CAPA | |
| `/app/audit/simulation` | Simulation | |
| `/app/audit/reports` | Final Audit Reports | |
| `/app/audit/final` | → reports | Alias |
| `/app/audit/planning` | Planning / calendar / annual plan | |
| `/app/audit/calendar` | → planning | Alias |
| `/app/audit/surveillance` | Surveillance MVP | |
| `/app/audit/assessment` | Chapter→Standard→OE tree + timeline/evidences | |

Plus `/app/auditor/*` (dashboard, sessions, workspace, evidence-review, observations, findings, reports, history).

---

## Not missing pages (never existed as Audit routes)

| Name | Reality |
|------|---------|
| Dedicated Internal Audit page | Always = dashboard |
| Dedicated Final Audit page | Always = `/app/audit/reports` |
| Audit Program entity page | SSOT = ScheduledAudit program metadata on Planning |
| Audit Checklist page | Lives in **Journey OE wizard** (operational mode Internal Audit) |
| Audit Execution page | = Auditor **Session Workspace** |
| Mock Audit page | Audit **type** only, not a screen |

---

## Related but different: Journey operational modes

Sidebar **Operational mode** switcher: Self Assessment / **Internal Audit** / Final Audit.  
That changes Journey/Evidence/Findings behavior — it is **not** a separate `/app/audit/internal` product screen.

---

## If something still feels missing

Please name the screen by **what you see on it** (e.g. checklist grid, auditor pack, mock run). Candidate product gaps (new build, not restore):

1. Richer Internal Audit **program hub** (beyond dashboard KPIs)
2. Filtered **Internal-type only** sessions/schedule view
3. Deeper checklist UX still owned by Journey under Internal Audit mode

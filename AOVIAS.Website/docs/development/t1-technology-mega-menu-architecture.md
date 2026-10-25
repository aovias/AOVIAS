# T1 — Technology Mega Menu Architecture

**Date:** 2026-07-20

---

## Columns (from registry)

| Column | Topics |
| --- | --- |
| Platform | Overview, Architecture, Platform Services, Technology Roadmap |
| Intelligence | AI at AOVIAS, AI Principles, AI Capabilities, Responsible AI |
| Trust | Security, Privacy & Trust, Integrations, APIs |
| Developer | Developer Platform, SDKs, Documentation → `/resources/docs` |

CTA: **Explore Technology** → `/technology`

Placeholder/future items include “(Coming soon)” in the description string for clarity while remaining navigable.

## Panel layout

[`mega-menu.tsx`](../../apps/website/src/components/navigation/mega-menu.tsx) widens to 56rem and uses up to four columns on `xl` when a mega menu has more than three columns (Technology).

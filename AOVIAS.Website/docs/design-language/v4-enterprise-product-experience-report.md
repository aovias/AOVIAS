# Phase V4 — Enterprise Product Experience

## Summary

Products landing and detail pages were rebuilt as a commercial portfolio experience. High-fidelity UI mockups replace placeholder diagrams where products are shown. Platform is now a first-class product at `/products/platform`.

## Delivered

### Products landing (`/products`)
- Commercial hero with Command mission-control UI
- Portfolio statement + **one** ecosystem illustration (Command → Platform → Studio → Customer)
- Three commercial product cards (purpose, audience, primary value, CTA, artwork)
- How they work together (narrative)
- Shared foundation cards (Identity, Security, Deployment, AI, Governance)
- Premium comparison table
- Closing CTA (Explore / Request demo)
- Browse taxonomy and roadmap removed from the landing (routes remain)

### AOVIAS Command (`/products/command`)
- Mission-control personality (`enterprise` atmosphere)
- Dashboard hero media via `CommandMissionControl`
- Workflows: Detect → Investigate → Approve → Deploy → Observe

### AOVIAS Studio (`/products/studio`)
- Editorial personality (`knowledge` atmosphere)
- Authoring workspace via `StudioAuthoringWorkspace`
- Lifecycle: Model → Review → Publish → Version → Release

### AOVIAS Platform (`/products/platform`)
- New authored product detail page
- Services console mockup + one architecture illustration
- Foundation capabilities: Identity, AI, Deployment, Security, Integration
- Nav mega-menu and catalog updated

### Mockups
- `CommandMissionControl` — ops chrome, metrics, incident table, workflow strip
- `StudioAuthoringWorkspace` — library/structure, document canvas, publish lifecycle
- `PlatformServicesConsole` — foundation services table and capability flow
- `ProductEcosystemFlow` — single commercial ecosystem diagram
- `media-helpers` routes product labels to high-fidelity UIs

## Out of scope (preserved)
- Homepage structure (platform card href only → `/products/platform`)
- Technology hub pages (unchanged; commercial Platform lives under Products)
- Company pages

## Verification
Run from `apps/website`: `npm run lint`, `npm run typecheck`, `npm run build`.

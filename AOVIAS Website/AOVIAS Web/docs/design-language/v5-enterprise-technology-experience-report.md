# Phase V5 — Enterprise Technology Experience

## Summary

The five Technology hubs were rebuilt as an **Enterprise Technology Library** — architecture-guide pages for technical evaluators, not marketing pages with technical vocabulary.

## Shared framework

`features/technology/editorial/`

- `TechnologyEditorialTemplate` — shared editorial rhythm
- `TechnologyStoryFlow` — vertical system story
- Content modules (`.tsx`) for each pillar

**Rhythm on every hub:**  
Hero → Core principle → One architecture diagram → Technical story → Practice cards → One workflow → Capability highlights → Single closing CTA

## Hubs delivered

| Hub | Route | Atmosphere | Signature visual |
|-----|-------|------------|------------------|
| Platform | `/technology/platform` | `blueprint-grid` | PlatformOrchestration + ArchitectureDiagram |
| AI | `/technology/ai` | `ai-neural-network` | HumanAiCollaboration + GovernanceWorkflow |
| Engineering | `/technology/engineering` | `architecture` | EngineeringLifecycle + ArchitectureDiagram |
| Security | `/technology/trust` | `security-geometry` | SecurePlatform + IdentityPlatformVisual |
| Integrations | `/technology/integrations` | `infrastructure-mesh` | IntegrationEcosystem + ApiFirstPlatform |

## Content posture

- Short editorial statements (3–4 line budget)
- Principles over implementation internals
- One memorable diagram per page
- One workflow per page
- AI illustrated as human → review → accountability (no neon / robot clichés)
- Security as designed layers (no lock/hacker tropes)
- Integrations as architecture (no API logo walls)

## Out of scope (preserved)

- Technology landing overview structure
- Child pages (platform/overview, AI principles, engineering reliability, etc.)
- Products and Company sections

## Verification

`npm run typecheck`, `npm run lint`, `npm run build` from `apps/website`.

# AccredaOne Workspace Landing Page v1.0

Authenticated workspace homepage at `/` (App Router). Discovery-first layout for **Hospital Quality Manager / Accreditation Lead** personas.

Built on **Design System v1.0 (closed baseline)** and **Application Shell v1.0**. All styling uses canonical CSS tokens via shared `.ao-*` classes — no feature-specific colour palette.

## Purpose

Communicate AccredaOne as an accreditation **knowledge discovery and explanation** platform, route users into the existing discovery workflow, and set honest expectations about connected vs planned framework content.

This is not the public marketing site.

## Section hierarchy

| Order | Section | ID / anchor | Component |
| ----- | ------- | ----------- | --------- |
| A | Welcome / hero | `#overview` | `WelcomeHero` |
| B | Knowledge discovery | `#library` | `KnowledgeDiscoveryPanel` (`presentation="workspace"`) |
| C | Framework knowledge | `#frameworks` | `FrameworkExplorer` |
| D | Knowledge shortcuts | — | `KnowledgeShortcuts` |
| E | Featured resources | — | `FeaturedResources` |
| F | Sponsored content | — | `SponsoredContent` (hidden when empty) |
| G | Knowledge integrity | — | `KnowledgeIntegrityNotice` |
| — | Presentation states (engineering) | `#states` | `PresentationStates` (preserved) |

Orchestration: `components/workspace/WorkspaceHome.tsx`.

## Search integration

- **Single discovery implementation:** `KnowledgeDiscoveryPanel` — same form, API (`postDiscoveryQuery`), framework filter, loading/empty/error/results, requirement detail.
- **Workspace mode:** alternate headings and placeholder; omits redundant “What this does” block; class `ao-discovery-workspace` for full-width panel.
- **Header / hero / framework explore:** `navigateToDiscoverySearch()` and `navigateToDiscoveryFramework()` in `lib/shell/discoverySearch.ts` (sessionStorage prefill + `#library`).

## Framework availability rules

Data: `lib/workspace/frameworkCatalog.ts`.

| Status | Meaning | UI |
| ------ | ------- | -- |
| `planned` | Product direction (e.g. NABH, JCI, NABL) | Badge “Planned”; no explore action |
| `connected_synthetic` | Searchable in development synthetic pack (NOAF, MCQF) | Badge “Development synthetic”; “Explore in discovery” sets framework prefill |
| `not_connected` | Reserved for future use | Badge “Not connected” |

No invented counts, requirements, or publisher claims.

## Sponsored content boundaries

- Component: `SponsoredContent` — renders **only** when `placements.length > 0`.
- Source: `lib/workspace/sponsoredContent.ts` (currently empty array).
- No advertising backend, no fictional sponsors, explicit “Sponsored” labelling when shown.

## Responsive behaviour

Uses existing shell breakpoint (960px) plus landing grids:

- **Desktop:** 3-column framework/featured grids; full hero; discovery panel full main width.
- **Tablet (≤1024px):** 2-column grids.
- **Mobile (≤639px):** single column; stacked hero CTAs; touch-friendly buttons (44px min on discovery actions).

`html { overflow-x: clip }` from shell retained.

## Future extension points

1. Add real NABH/JCI/NABL connectors → update catalog availability and explore actions.
2. Wire `FEATURED_RESOURCES` to CMS; remove sample badges when live.
3. Populate `SPONSORED_PLACEMENTS` from configured feed.
4. Add Saved Knowledge / library catalogue shortcuts when routes exist.
5. Personalization in `KnowledgeShortcuts`.

## Component responsibilities

See `components/workspace/*` — compose shared tokens (`.ao-card`, `.ao-btn`, `.ao-badge`, `.ao-kicker`) only; no inline colours in TSX.

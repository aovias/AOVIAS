# 04 — Background & Atmosphere System (EBAS)

**AVLS v1** · Architecture only (no new page wiring in V1)

## Purpose

EBAS is the reusable library of named atmospheres. Pages declare an atmosphere **type**; they do not invent one-off decorative CSS.

## Governing frames

- `01_round1_foundation/02_background_atmosphere_system.jpeg` — nine-panel library  
- `02_phase1_color_and_refinement/e4_hero_illustration_rules.jpeg` — page → atmosphere mapping  
- Existing implementation substrate: `EnterpriseAtmosphere` / `enterpriseAtmosphereRegistry` (already in codebase; V2 aligns naming)

## Composition model

Each atmosphere may combine:

1. Base surface gradient (Void / Parchment / Graphite / Inverse)  
2. Pattern plane (grid, mesh, filaments, rings, lanes, topo)  
3. Soft lighting (aurora / mist / horizon glow)  
4. Optional silhouette marks (frame, diamond, rings, nodes)  
5. Domain-accent wash (low opacity; respects one-accent rule)

**Never:** heavy photos as wallpaper, stock people, neon particles, competing multi-hue washes.

## Canonical registry (AVLS names)

| AVLS ID | Creative Direction label | Intent | Theme notes |
|---------|--------------------------|--------|-------------|
| `enterprise` | Enterprise Abstract | Institutional depth, soft void | Light: cool muted; Dark: canvas void |
| `aurora` | Platform Atmosphere / Void mist | Invitation, soft light | Dual radial glow |
| `blueprint` | Blueprint | Engineering precision | Cyan-grey grid on void |
| `workflow` | Workflow | Operational lanes / handoffs | Horizontal process bands |
| `architecture` | Infrastructure Mesh (structural) | Layered systems | Strata + seams |
| `knowledge` | Knowledge Pattern | Editorial / findability | Topographic / ruled lines |
| `security` | Security Geometry | Protection bounds | Hex / concentric rings, Vault Bronze |
| `ai` | AI Network | Assistive intelligence | Signal Blue filaments / nodes |
| `platform` | Platform Atmosphere / mesh | Shared foundation | Dot lattice + depth |
| `corporate` | Corporate Gradient | Institutional authority | Smooth graphite/navy planes |
| `minimal` | Graphite / Minimal Neutral | Pricing, legal, quiet | Gradient only / column rule |

### Page → atmosphere defaults (from e4)

| Page family | Atmosphere |
|-------------|------------|
| Homepage | `enterprise` + platinum lines (aurora allowed as secondary wash) |
| AI | `ai` |
| Security / Trust | `security` |
| Engineering | `blueprint` |
| Company | `corporate` on parchment inverse where specified |
| Pricing | `minimal` (no illustration) |

Full assignment matrix: [registries/ebas.md](./registries/ebas.md)

## Density

| Density | Use |
|---------|-----|
| `subtle` | Mid-page bands, auth, legal |
| `default` | Secondary shells |
| `rich` | Page heroes |

## Light / dark

Atmospheres are defined once via semantic tokens. Themes remap canvas, border, and accent — no duplicate art assets required for V1 architecture.

## Implementation stance (V1)

- **Define** registry IDs, intents, mappings  
- **Do not** change page call-sites in this sprint  
- Existing `EnterpriseAtmosphere` keys should be **aliased** to AVLS IDs in V2 (compatibility table in registry doc)  

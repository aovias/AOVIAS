# EBAS Registry (AVLS)

Architecture & catalog only — **not applied to pages in Phase V1**.

## Variants

| ID | Label | Intent | Light | Dark | Density default |
|----|-------|--------|-------|------|-----------------|
| `enterprise` | Enterprise Abstract | Institutional void + soft depth | Cool muted canvas | `#0A0E14` void | rich (heroes) |
| `aurora` | Aurora / Platform mist | Invitation, soft light | Soft dual glow | Soft dual glow | rich |
| `blueprint` | Blueprint Grid | Engineering precision | Cyan-grey grid | Grid on void | rich |
| `workflow` | Workflow | Operational lanes | Lane bands | Lane bands | rich |
| `architecture` | Architecture | Layers & boundaries | Strata + seams | Strata + seams | rich |
| `knowledge` | Knowledge Pattern | Editorial findability | Topo / rules | Dim topo | rich |
| `security` | Security Geometry | Protection | Bronze rings/hex | Bronze on void | rich |
| `ai` | AI Network | Intelligence filaments | Signal blue filaments | Filaments on void | rich |
| `platform` | Platform | Shared foundation mesh | Dot lattice | Lattice on void | rich |
| `corporate` | Corporate Gradient | Institutional authority | Smooth planes / parchment | Navy gradient | rich |
| `minimal` | Minimal / Graphite | Quiet commercial | Soft graphite | Graphite void | subtle |

## Compatibility with live `EnterpriseAtmosphere` keys

| AVLS ID | Existing key (approx) |
|---------|----------------------|
| `enterprise` | `enterprise-abstract` |
| `aurora` | `aurora` |
| `blueprint` | `blueprint-grid` |
| `workflow` | `workflow` |
| `architecture` | `architecture` |
| `knowledge` | `knowledge-pattern` |
| `security` | `security-geometry` |
| `ai` | `ai-neural-network` |
| `platform` | `platform-grid` / `technology-atmosphere` |
| `corporate` | `corporate-gradient` |
| `minimal` | `subtle-texture` / `minimal-neutral` |
| (mesh) | `infrastructure-mesh` → treat as `platform` or `architecture` subvariant in V2 |

## Page assignment (target)

| Page family | Atmosphere |
|-------------|------------|
| Home | `enterprise` (+ optional `aurora` wash) |
| Company / Careers | `corporate` |
| Contact | `aurora` |
| Products / Product detail | `workflow` |
| Technology landing | `platform` |
| Platform pages | `platform` / `architecture` |
| Engineering | `blueprint` |
| AI | `ai` |
| Security / Trust | `security` |
| Identity / Privacy | `security` or trust rings variant |
| Resources | `knowledge` |
| Pricing | `minimal` |
| Legal | `minimal` |
| Auth panel | trust / `corporate` subtle |

## Declaration API (future V2)

```ts
atmosphere: AvlfAtmosphereId // e.g. 'ai'
density?: 'subtle' | 'default' | 'rich'
```

Do not embed custom background CSS in pages.

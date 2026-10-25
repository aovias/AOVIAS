# AOVIAS Brand Illustration Language

SVG-first, blueprint-inspired illustrations for the Public Website.

## Design principles

1. Every illustration answers: **What is AOVIAS trying to explain?**
2. Prefer explanation over decoration.
3. One family: shared stroke, radius, opacity ladder, and grid module.
4. Light/dark via `currentColor` and existing theme tokens — no design-system token renames.

## Family contract

| Token (code) | Value |
| --- | --- |
| Stroke | 1 / 1.25 / 1.5 / 2 |
| Radius | 6 / 8 |
| Fill opacity | 0.05 → 0.14 (accent ~0.18) |
| Grid | `BrandGrid` step 20–28 |

Primitives live in `apps/website/src/components/brand/brand-primitives.tsx`.

## Inventory

| ID | Component | Explains |
| --- | --- | --- |
| `orchestration` | `PlatformOrchestration` | Platform → products → institutions (home hero) |
| `ecosystem` | `PlatformEcosystem` | Platform → products → institutions → people → society |
| `architecture` | `ArchitectureDiagram` | Experience / control / delivery / foundation |
| `command` | `CommandOperations` | Operational workspace |
| `studio` | `StudioDelivery` | Delivery workspace |
| `workflow` | `WorkflowPanel` | Process steps |
| `domain-*` | `DomainScene` | Sector & capability scenes |
| `cover` | `KnowledgeCover` | Knowledge card covers |
| `mark` | `BrandMark` | Geometric lockup |
| `atmosphere` | `BrandAtmosphere` | Blueprint background motif |

### DomainScene variants

`healthcare`, `government`, `education`, `enterprise`, `security`, `trust`, `operations`, `architecture`, `ai`, `deployment`, `monitoring`, plus marketing: `mission`, `contact`, `careers`, `ecosystem`, `industry`.

## Usage

- Marketing heroes: `Hero illustration={<… />}`
- Product MediaSlots: `productMediaSlot(label)` → `kind: 'custom'`
- Knowledge cards: `ContentImage` `kind: 'cover'` → `KnowledgeCover`

Do not introduce dashed placeholders on public surfaces.

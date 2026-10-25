# 14 — Domain Identity System

**AVLS v1** · Source: `e3_product_domain_colors_v1` + existing `DomainAccentScope`

## Rule

**One accent per viewport.** Domain color appears on icons, badges, section markers, diagram signals, and restrained CTAs — never as a full-page tint.

## Products

| ID | Accent | Notes |
|----|--------|-------|
| command | `#4A6FA5` | Operations blue |
| studio | `#D9CFC0` | Editorial cream (restrained) |
| platform | Technology / indigo family | Shared foundation |

## Domains

| ID | Accent | Notes |
|----|--------|-------|
| technology | `#6B7A8F` | |
| ai | `#4A6FA5` | Signal blue filaments |
| engineering | `#5A7A85` | Blueprint cyan-grey companion |
| security | `#8A6F4A` | Vault Bronze rings |
| identity / trust | Teal-adjacent / supporting | Prefer trust geometry atmosphere |
| resources / knowledge | Slate | Knowledge pattern |
| company | Neutral / parchment | Corporate gradient |
| industries | Sector accents (restrained) | Healthcare emerald, gov navy, etc. |

## Implementation substrate

Keep using path-based `data-product` / `data-technology` / `data-industry` → `--domain-accent`.

Phase V2: reconcile Creative Direction hex intents with live semantic tokens without breaking contrast.

## Anti-patterns

- Mixing Command blue + Studio cream + Security bronze in one hero  
- Recoloring body text to domain accent  
- Domain-colored backgrounds behind long-form reading  

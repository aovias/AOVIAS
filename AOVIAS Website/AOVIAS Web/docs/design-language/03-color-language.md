# 03 — Color Language

**AVLS v1** · Primary sources: `e1_surface_hierarchy`, `e2_buttons_states`, `e3_product_domain_colors_v1`, `e4_hero_illustration_rules`

## Philosophy

Neutrals form the foundation. Color is **signal**. Primary brand blue is reserved for emphasis and interactive hierarchy — not page wash.

## Surface & elevation (dark institutional stack)

From style-guide page 1 (`e1`):

| Token name (AVLS) | Hex | Role |
|-------------------|-----|------|
| Canvas | `#0A0E14` | Base void / footer field |
| Surface 01 | `#1A1F2A` | Header / first elevation |
| Surface 02 | `#232A36` | Cards / panels |
| Surface 03 / Elevated | `#2E3542` | Raised interactive surfaces |
| Inverse Canvas | `#E8ECEF` | Light editorial / inverse bands |
| Editorial Accent | `#D9CFC9` | Parchment / institutional warmth |
| Signal / Singular Accent | `#4A6FA5` | Primary interactive blue |

> **Engineering note:** Map these intents to existing semantic tokens (`--background`, `--surface-*`, `--primary`, etc.) in Phase V2. Do not hardcode hex in page components.

## Product colors

| Product | Accent hex | Illustration cue |
|---------|------------|------------------|
| AOVIAS Command | `#4A6FA5` | Isometric terminal / operations console |
| AOVIAS Studio | `#D9CFC0` | Isometric workstation + blueprints |

## Domain accents

| Domain | Accent hex | Icon cue |
|--------|------------|----------|
| Technology | `#6B7A8F` | Chip / circuit |
| AI | `#4A6FA5` | Neural / brain node |
| Engineering | `#5A7A85` | Calipers + gear |
| Security | `#8A6F4A` | Shield + lock (Vault Bronze) |

### Atmosphere inks (from e4)

| Ink | Usage |
|-----|-------|
| Platinum | Structural lines, horizons |
| Signal Blue | AI filaments / focus nodes |
| Vault Bronze | Security rings |
| Blueprint Cyan-Grey | Engineering grids |
| Graphite | Pricing / minimal gradients |
| Parchment | Company institutional |

## Interactive color (from e2)

| Tier | Default | Notes |
|------|---------|-------|
| Primary | `#4A6FA5` | Hover lighten + soft glow; Active deepen |
| Secondary | Dark `#333` + light border | Ghost/enterprise outline family |
| Tertiary | Text-only | Underline on hover |
| Destructive | `#B67B4A` | Earth / caution — not neon red |
| Status Operational | `#6A9E9F` | |
| Status Degraded | `#B67B4A` | |
| Status Incident | `#A65A5A` | |
| Status Maintenance | `#C0B290` | |

## Governance

- **ONE ACCENT PER VIEWPORT** (e3)  
- Illustration lines: Platinum @ ~60% opacity; **one** domain-accent highlight only (e4)  
- No decorative multi-stop marketing gradients unless approved as an EBAS asset  

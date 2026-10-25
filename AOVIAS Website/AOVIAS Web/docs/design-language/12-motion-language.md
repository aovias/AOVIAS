# 12 — Motion Language

**AVLS v1** · Sources: Guidelines/07 + `n7`, `n8`, `n10_signature_moments`

## Philosophy

Motion explains, guides, confirms, orients, reveals, or connects.  
**Never** entertains.

Feel: calm, professional, precise, deliberate, confident.  
Never: playful, bouncy, chaotic, game-like.

## Timing (Guidelines)

| Token | Duration |
|-------|----------|
| Instant | 100ms |
| Fast | 150ms |
| Standard | 250ms |
| Slow | 350ms |

Signature moments may use up to **800ms** when narrative (Horizon Reveal).

## Signature moments (`n10`)

| Moment | Behavior | Duration | Apply to |
|--------|----------|----------|----------|
| **Horizon Reveal** | Platinum 1px line draws; content fades in | 800ms | Home / enterprise heroes |
| **Node Focus** | Hovered node accent; others dim to 30%; tooltip | hover | Diagrams / meshes |
| **Audit Trail Live** | Row slides up; azure pulse → steady edge | short | Command / governance tables |
| **Silent Confirmation** | Button platinum → muted teal; check fades in | 400ms | Forms / request CTAs |

## Section transitions (`n7`)

- Soft fade / short translate  
- No parallax storms  
- Honor `prefers-reduced-motion`  

## Micro-interactions (`n8`)

- Link underline grow  
- Icon color to signal  
- Card lift ≤ 4px  

## Mapping

Existing: `MarketingReveal`, `motion/hover`, Framer Motion transitions — extend; do not introduce a second motion library.

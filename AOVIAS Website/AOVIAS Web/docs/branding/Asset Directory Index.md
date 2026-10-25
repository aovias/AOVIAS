# AOVIAS Asset Directory Index

**Root:** `assets/brand/`  
**Export:** `node assets/brand/_scripts/export-all.mjs` (run from `apps/website` so `sharp` resolves)

| Directory | Contents |
| --- | --- |
| `_masters/` | Editable SVG sources (logos, icons) |
| `_scripts/` | `export-all.mjs` rasterization + public copy |
| `logos/` | Corporate + `products/` lockups (SVG/PNG/PDF companions) |
| `icons/` | Product icons SVG + per-product size folders (16–512) |
| `favicons/` | Multi-size PNG + SVG mark |
| `app-icons/` | web, pwa (maskable/monochrome), android, ios, windows, macos |
| `social/og/` | Open Graph 1200×630 SVG+PNG |
| `patterns/` | Grid, blueprint, architecture lines, institutional geometry |
| `illustrations/` | Domain/platform SVG library |
| `diagrams/` | Architecture diagram SVG set |
| `mockups/` | Blueprint workspace mockups |
| `documentation/` | Banner, badges, headers |
| `presentations/` | Cover, divider, backgrounds |
| `email/` | Header, footer, notification logo |

**Website runtime:** `apps/website/public/brand|og|icons` + `manifest.webmanifest`

**Brand docs:** `docs/branding/`

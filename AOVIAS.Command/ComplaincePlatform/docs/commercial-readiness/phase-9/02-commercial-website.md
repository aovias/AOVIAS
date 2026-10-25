# 02 — Commercial Website

**Phase:** 9.2  
**Sprint:** 2  
**Date:** 2026-07-26

## Delivered

Static marketing site under `commercial-launch/website/`:

| File | Purpose |
|------|---------|
| `index.html` | Brand-forward home |
| `healthcare.html` | Healthcare / accreditation positioning |
| `features.html` | Capability overview by entitlement |
| `pricing.html` | SKU table → entitlement codes (no payment) |
| `docs.html` | Index to published guides |
| `demo.html` | Demo request (mailto + non-posting form) |
| `contact.html` | Sales contact (mailto + non-posting form) |
| `styles.css` | Teal/slate accessible theme |

## Standards applied

- SEO meta + Open Graph basics
- Skip link to `#main`
- Analytics placeholder HTML comment (no third-party script until approved)
- Accessible focus styles; reduced-motion respect
- Pricing references `core.compliance`, `ai.assist`, `governance.risk`, `ace.insights`
- Contact: `sales@aovias.example`

## Hosting note

Serve the folder as static files (Azure Static Web Apps, S3+CDN, nginx). Update absolute canonical URLs from `aovias.example` before public DNS cutover. Wire form `action` endpoints when CRM is ready.

## Exit

Website pack ready for staging publish; no product feature code changed.

# 1 — Global Experience Integration Report

**Phase:** V2 GEI · Release 1.1  
**Status:** Complete  
**Scope:** Shared application shell only (no page redesign)

## Objective

Implement the global visual “operating system” that every route inherits — spacing rhythm, atmosphere request, transitions, and section hierarchy — without changing layout architecture, content, or routing.

## Changes

| Surface | Change |
|---------|--------|
| `AppShell` | Accepts `atmosphere` (AVLS ID), optional `injectAtmosphere`, `data-layout-atmosphere`, wraps `LayoutAtmosphereProvider` + `ShellTransition` |
| `MarketingLayout` | Default `enterprise`; overridable (company/solutions → `corporate`) |
| `ProductLayout` | Default `workflow` |
| `TechnologyLayout` | Default `blueprint` |
| `KnowledgeLayout` | Default `knowledge` |
| `DocumentationLayout` | Default `knowledge` + subtle inject |
| `LegalLayout` | Default `minimal` + subtle inject (local duplicate atmosphere removed) |
| `AuthenticationLayout` | Declares `security`; panel retains `trust-geometry` |
| `ErrorLayout` | Declares `minimal` |

## Non-goals (honored)

- No homepage visual transformation (V3)
- No content or IA changes
- No page-specific artwork
- No layout architecture rewrites

## Verification

- Lint / typecheck / build passed
- Production server on `localhost:3000`
- Route samples confirm `data-layout-atmosphere` wiring (home=`enterprise`, products=`workflow`, technology=`blueprint`, company=`corporate`, resources hub=`knowledge`, legal=`minimal`)

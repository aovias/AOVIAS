# 4 — Enterprise Search Report

**Phase:** V2 GEI · Release 1.1  
**Status:** Complete  
**AVLS refs:** `10-navigation-language.md` (n5)

## Capabilities

- Ctrl + K / ⌘ + K hotkey (single header instance)
- Search button in navigation (`SearchEntry` button variant)
- Keyboard navigation (↑↓, Enter, Escape)
- Category grouping: Products, Technology, Industries, Resources, Company, Legal, Pricing, Contact
- Knowledge platform deep-link fallback (`/resources/search?q=`)

## Architecture (R2-ready)

| Piece | Role |
|-------|------|
| `registries/avlf/search.ts` | Static registry + `SearchProvider` interface |
| `staticSearchProvider` | Default Release 1.1 provider |
| `setSearchProvider()` | Swap for Release 2 remote/indexed backends |
| `EnterpriseSearchOverlay` | UI / motion / a11y |
| `SearchEntry` | Trigger + hotkey |

## Files

- `registries/avlf/search.ts`
- `components/search/enterprise-search-overlay.tsx`
- `components/navigation/search-entry.tsx`

## Outcome

Command-palette search is enterprise-grade and provider-swappable without page redesign.

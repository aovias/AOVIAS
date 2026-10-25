# Visual Component Mapping

Maps AVLS concepts → **existing** components. Do not duplicate.

| AVLS concept | Existing component(s) | Integration notes |
|--------------|----------------------|-------------------|
| Atmosphere / EBAS | `EnterpriseAtmosphere`, `BrandAtmosphere`, `HomeHeroAtmosphere` | Declare named variant; alias AVLS IDs in V2 |
| Hero | `Hero`, `ProductHero` | `atmosphere` string + MediaSlot illustration |
| Section rhythm | `SectionSurface` | Optional `atmosphere` for mid-page; prefer registry IDs |
| Illustration shell | `BrandShell`, `BrandGrid`, `BrandPanel`, brand primitives | Stroke/opacity via illustration ink tokens |
| Product visuals | `CommandOperations`, `StudioDelivery`, `ProductPair`, product consoles | Product accents via domain scope |
| Platform / tech diagrams | `PlatformOrchestration`, `PlatformEcosystem`, `PlatformIntelligence`, `ArchitectureDiagram` | Diagram registry styles |
| Domain scenes | `DomainScene`, `InstitutionalScene` | Company / industries |
| Trust / security visuals | `SecurePlatform`, `TrustEcosystem`, `IdentityPlatformVisual`, … | Security / trust atmospheres |
| Knowledge visuals | `KnowledgeCover` | Knowledge pattern atmosphere |
| Cards | `Card` (+ `accent`), `FeatureCard`, `ValueCard`, `PrincipleCard`, `PricingCard`, `ArticleCard` | Card language modes |
| CTA | `CtaSection`, `MarketingCtaLink`, Button | Dark close band; e2 hierarchy |
| Navigation | `HeaderChrome`, mega menu, search | n1–n5 |
| Footer | `SiteFooter` | Enterprise footer language |
| Trust signals | `TrustBadge` | Semantic variant tints |
| Motion | `MarketingReveal`, `lib/motion/hover` | Signature moments in V2 |
| Domain accent | `DomainAccentScope`, `resolveDomainAccentAttrs` | One accent / viewport |
| Surfaces / color | Design System `themes.css` tokens | Map e1 intents — no parallel token tree |
| Forms | Input, Textarea, Select | Contact clarity; Silent Confirmation later |
| Media | `MediaSlot` helpers | Prefer registry illustration IDs |

## Explicit non-actions

- Do not create a second Card or Hero system for “creative” variants  
- Do not bypass semantic tokens with raw hex in feature pages  
- Do not add stock photo components as brand defaults  

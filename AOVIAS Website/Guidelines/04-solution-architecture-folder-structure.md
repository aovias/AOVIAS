---
title: "AOVIAS Website Solution Architecture & Folder Structure"
source: appflowy
workspace: AOVIAS Website
space: General
view_id: "4514dad4-ad61-4f8b-bb74-7f98d8a967f4"
exported_at: "2026-07-19"
---

# AOVIAS Website Solution Architecture & Folder Structure
### Purpose

This document defines the official solution architecture and directory organization for the AOVIAS Public Website.

The objective is to ensure:

- Long-term maintainability

- Predictable organization

- Clear architectural boundaries

- Reusable modules

- Autonomous AI development compatibility

- Enterprise scalability

The directory structure is a long-term architectural contract and shall not be reorganized without documented architectural justification.

---

## 1. Architectural Principles

The solution shall follow these principles:

- Feature-oriented organization over file-type organization.

- Shared resources are centralized.

- Business capabilities remain isolated.

- Components are composed rather than duplicated.

- Domain boundaries are explicit.

- Every directory has a single responsibility.

---

## 2. High-Level Solution Structure

aovias-web/
│
├── apps/
├── packages/
├── content/
├── infrastructure/
├── tooling/
├── scripts/
├── docs/
├── public/
├── .github/
├── docker/
└── README.md

---

## 3. Applications

apps/
│
└── website/

Future applications may include:

apps/
│
├── website/
├── customer-portal/
├── partner-portal/
├── developer-portal/
├── academy/
└── admin/

Even if only website exists today, the structure anticipates future growth.

---

## 4. Website Source Structure

apps/
└── website/
    │
    ├── src/
    ├── public/
    ├── tests/
    ├── package.json
    ├── next.config.ts
    └── tsconfig.json

---

## 5. Source Structure

src/
│
├── app/
├── components/
├── features/
├── layouts/
├── content/
├── services/
├── lib/
├── hooks/
├── providers/
├── styles/
├── config/
├── constants/
├── types/
├── utils/
├── middleware/
└── assets/

---

## 6. App Router

app/
│
├── (marketing)/
├── (products)/
├── (resources)/
├── (company)/
├── (legal)/
├── (auth)/
├── api/
├── sitemap.ts
├── robots.ts
├── layout.tsx
├── not-found.tsx
└── page.tsx

Each route group represents a business capability rather than a visual section.

---

## 7. Components

components/
│
├── primitives/
├── layout/
├── navigation/
├── hero/
├── branding/
├── content/
├── products/
├── pricing/
├── trust/
├── forms/
├── auth/
├── feedback/
├── marketing/
├── resources/
├── legal/
└── shared/

Components shall not contain business logic.

---

## 8. Features

Business logic lives here.

features/
│
├── authentication/
├── pricing/
├── checkout/
├── newsletter/
├── contact/
├── careers/
├── search/
├── blog/
├── documentation/
├── downloads/
├── analytics/
└── seo/

Each feature owns:

- Business logic

- Validation

- API interactions

- Domain models

- Feature-specific components

---

## 9. Layouts

layouts/
│
├── marketing/
├── products/
├── documentation/
├── blog/
├── legal/
└── authentication/

Layouts define page chrome, not page content.

---

## 10. Content

content/
│
├── blog/
├── changelog/
├── documentation/
├── case-studies/
├── products/
├── careers/
└── resources/

Content remains version-controlled alongside the codebase.

---

## 11. Services

services/
│
├── identity/
├── contact/
├── newsletter/
├── analytics/
├── search/
├── pricing/
├── checkout/
└── api/

Services communicate with external APIs and platform services.

---

## 12. Shared Libraries

lib/
│
├── markdown/
├── seo/
├── images/
├── motion/
├── validation/
├── accessibility/
├── telemetry/
└── performance/

These are shared utilities rather than business features.

---

## 13. Providers

providers/
│
├── theme/
├── analytics/
├── authentication/
├── toast/
└── query/

Only global application providers belong here.

---

## 14. Styles

styles/
│
├── globals.css
├── typography.css
├── utilities.css
├── animations.css
└── tokens.css

The design tokens defined in the Design Language document should map directly into tokens.css (or Tailwind theme configuration).

---

## 15. Configuration

config/
│
├── navigation.ts
├── footer.ts
├── products.ts
├── social.ts
├── seo.ts
├── analytics.ts
└── site.ts

Configuration should be centralized and strongly typed.

---

## 16. Constants

constants/
│
├── routes.ts
├── colors.ts
├── breakpoints.ts
├── animations.ts
└── metadata.ts

Magic values should not be scattered through the project.

---

## 17. Types

types/
│
├── api.ts
├── products.ts
├── navigation.ts
├── seo.ts
├── content.ts
└── common.ts

Types should be organized by domain, not by framework.

---

## 18. Assets

assets/
│
├── illustrations/
├── icons/
├── logos/
├── patterns/
└── animations/

Assets should be versioned and categorized.

---

## 19. Packages

For shared code across future applications:

packages/
│
├── ui/
├── design-tokens/
├── eslint-config/
├── typescript-config/
├── branding/
├── icons/
└── utilities/

This allows the customer portal, partner portal, and future applications to share the same design language.

---

## 20. Infrastructure

infrastructure/
│
├── docker/
├── nginx/
├── deployment/
├── monitoring/
└── scripts/

Deployment artifacts remain separate from application code.

---

## 21. Documentation

docs/
│
├── architecture/
├── decisions/
├── design-system/
├── development/
├── deployment/
└── specifications/

Architecture decisions and development guides live alongside the codebase.

---

## 22. Naming Conventions

The structure follows these rules:

- Folders use kebab-case.

- React components use PascalCase.

- Hooks begin with use.

- Types are named by domain.

- Avoid framework-specific names in business folders.

Consistency is mandatory.

---

## 23. Ownership Rules

Each directory has one responsibility.

For example:

- components/ → presentation only.

- features/ → business capability.

- services/ → external communication.

- lib/ → reusable utilities.

- config/ → static configuration.

- content/ → authored content.

Crossing these boundaries requires architectural justification.

---

## 24. Final Principle

> The folder structure is an architectural map of the platform.

A developer—or an autonomous AI agent—should be able to understand where a new feature belongs without hesitation. If there is uncertainty about placement, the architecture should be refined rather than allowing ad hoc organization.

---

### One architectural refinement

Because AOVIAS already has a broader platform ecosystem (Studio, Command, Runtime, Identity, and future portals), I'd adopt a monorepo from day one, even if only the website application exists initially. That way, when you later add a customer portal or developer portal, you can reuse shared UI, design tokens, TypeScript configuration, and branding packages instead of extracting them after the fact. It aligns well with the platform-first strategy you've been following across AOVIAS.

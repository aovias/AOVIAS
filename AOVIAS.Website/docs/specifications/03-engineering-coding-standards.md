---
title: "AOVIAS Website Engineering & Coding Standards"
source: appflowy
workspace: AOVIAS Website
space: General
view_id: "719c8699-bf80-49d5-a752-c4f6c2c85529"
exported_at: "2026-07-19"
---

# AOVIAS Website Engineering & Coding Standards
### Purpose

This document establishes the mandatory engineering standards for the AOVIAS Public Website.

Its objectives are to ensure:

- Code consistency

- Maintainability

- Readability

- Scalability

- Performance

- Accessibility

- Autonomous AI compatibility

These standards apply to all contributors, whether human or AI.

---

## 1. Core Engineering Principles

Every implementation shall be:

- Simple

- Readable

- Predictable

- Reusable

- Testable

- Performant

- Strongly typed

- Production-ready

Code should be optimized for long-term maintainability rather than short-term convenience.

---

## 2. React Standards

The project shall exclusively use:

- Functional Components

- React Hooks

- Composition

- Server Components by default

- Client Components only when required

Class Components are prohibited.

---

## 3. Server-First Architecture

All pages and components shall be Server Components unless they require:

- User interaction

- Browser APIs

- Local state

- Effects

- Animation libraries that require the client

- Event handlers

The "use client" directive shall be treated as an exception, not the default.

---

## 4. TypeScript Standards

TypeScript strict mode is mandatory.

Requirements:

- strict: true

- noImplicitAny

- strictNullChecks

- exactOptionalPropertyTypes

- noUncheckedIndexedAccess

No implementation may weaken compiler strictness.

---

## 5. Prohibited TypeScript Practices

The following are prohibited unless explicitly justified:

- any

- @ts-ignore

- @ts-nocheck

- Unsafe type assertions

- Implicit any

- Disabling ESLint rules to bypass typing

If a type is difficult to express, create an appropriate interface or type rather than bypassing the type system.

---

## 6. Naming Conventions

Consistency is mandatory.

- Components: PascalCase

- Hooks: useCamelCase

- Variables: camelCase

- Constants: UPPER_SNAKE_CASE (only for true constants)

- Types & Interfaces: PascalCase

- Files: kebab-case

- Folders: kebab-case

Names should describe intent rather than implementation details.

---

## 7. Imports

Absolute imports are required.

Example:

@/components
@/features
@/lib
@/types
@/config

Avoid deep relative paths like:

../../../../components/button

---

## 8. Component Standards

Every component shall:

- Have a single responsibility.

- Be strongly typed.

- Accept minimal props.

- Avoid unnecessary re-renders.

- Support accessibility.

- Support dark mode (where applicable).

- Support responsive layouts.

Components should remain small and composable.

---

## 9. Business Logic

Business logic shall never reside in presentation components.

Presentation components may:

- Render UI

- Receive props

- Emit events

Business logic belongs in:

- Feature modules

- Services

- Custom hooks

- Server actions

---

## 10. Hooks

Custom hooks shall encapsulate reusable behavior.

Hooks should:

- Begin with use

- Be focused on a single concern

- Return a well-defined API

- Avoid hidden side effects

Do not use hooks to bypass proper architecture.

---

## 11. State Management

Prefer the simplest appropriate solution.

Order of preference:

1. Server Components

2. URL state

3. Local component state

4. React Context

5. External state libraries (only when justified)

Global state should not be introduced without clear architectural need.

---

## 12. Duplication

Code duplication is prohibited.

When similar implementations emerge:

- Extract shared logic.

- Create reusable utilities.

- Generalize components.

Duplication should be viewed as technical debt.

---

## 13. Error Handling

All operations shall handle failure gracefully.

Requirements:

- Meaningful user feedback

- Typed error handling

- Logging where appropriate

- Recovery paths when possible

Errors should never fail silently.

---

## 14. Configuration

Hard-coded values are prohibited.

Configuration belongs in:

- config/

- Environment variables

- Typed constants

Business rules should not be embedded directly in components.

---

## 15. Comments

Code should be self-explanatory.

Comments should explain:

- Why a decision exists

- Architectural rationale

- Non-obvious behavior

Comments should not describe what the code already makes clear.

---

## 16. Accessibility

Every interactive component shall support:

- Keyboard navigation

- Semantic HTML

- Focus management

- ARIA where appropriate

- Screen readers

Accessibility is part of implementation, not post-processing.

---

## 17. Performance

Developers shall:

- Avoid unnecessary client-side rendering.

- Minimize bundle size.

- Lazy-load where appropriate.

- Optimize images.

- Reduce hydration costs.

- Avoid unnecessary dependencies.

Performance should be considered during implementation.

---

## 18. Dependency Management

Every dependency must be justified.

Before adding a package, ask:

- Does the platform already solve this?

- Can this be implemented simply?

- Is the package actively maintained?

- Does it increase bundle size unnecessarily?

- Is it compatible with our licensing requirements?

Avoid dependency sprawl.

---

## 19. Testing Standards

Every reusable component should support:

- Unit testing (where practical)

- Accessibility verification

- Responsive validation

- Type safety

- Manual review

Critical business workflows should include end-to-end tests.

---

## 20. Logging

Logging should be:

- Structured

- Meaningful

- Free of sensitive data

Debug logging shall not remain in production code.

---

## 21. Security

Code shall:

- Validate all inputs.

- Escape user content.

- Avoid exposing secrets.

- Use secure defaults.

- Follow the principle of least privilege.

Security considerations should be part of implementation, not an afterthought.

---

## 22. Refactoring

Developers are encouraged to improve existing code when:

- Readability increases.

- Duplication decreases.

- Complexity is reduced.

Refactoring must not change observable behavior unless explicitly intended.

---

## 23. Definition of Clean Code

Before considering implementation complete, ask:

- Is this the simplest correct solution?

- Is the intent obvious?

- Can another developer understand it quickly?

- Does it fit the existing architecture?

- Is it reusable?

- Is it fully typed?

- Would I be comfortable maintaining this in five years?

If the answer to any question is No, the implementation should be improved.

---

## 24. Non-Negotiable Rules

The following are prohibited without explicit architectural approval:

- any

- Class Components

- Relative import chains

- Business logic in UI components

- Duplicate implementations

- Dead code

- Unused exports

- Disabled lint rules to bypass issues

- Breaking strict TypeScript

- Mixing presentation and domain logic

---

## 25. Final Engineering Principle

> Every line of code should strengthen the maintainability of the platform.

Code is not written merely to make the website function today. It is written to ensure that future developers—and future autonomous AI agents—can understand, extend, and evolve the platform with confidence while preserving the engineering standards of AOVIAS.

---

### One addition I'd strongly recommend

For AOVIAS, I'd add a companion document called Architecture Decision Records (ADR) Guidelines. Whenever a significant engineering choice is made—such as introducing a new dependency, changing authentication, or altering routing—it should be captured in a short ADR. Over time, this creates a valuable engineering history, explaining not just what the architecture is, but why it evolved that way.

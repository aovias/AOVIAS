---
title: "First Plan"
source: appflowy
workspace: AOVIAS Website
space: General
view_id: "946b660b-06e9-4651-b14e-5f04e111746c"
exported_at: "2026-07-19"
---

# First Plan
#### 1. Execution Rules (Highest Priority)

Tell the agent exactly how to work.

For example:

- Never replace an existing implementation without justification.

- Preserve consistency across all pages.

- Build reusable components before page-specific ones.

- Never duplicate components.

- Favor composition over repetition.

- Every component must be responsive and accessible.

- Every change must compile without errors.

- Every commit must leave the project in a runnable state.

These rules prevent the agent from drifting.

---

#### 2. Definition of Done

For every page, require that it meets measurable criteria.

Example:

- Responsive from 320px to 4K.

- Lighthouse ≥95.

- No accessibility violations.

- Dark mode supported.

- Keyboard navigation complete.

- SEO metadata included.

- Open Graph metadata.

- JSON-LD structured data where applicable.

- Animations respect reduced-motion preferences.

- TypeScript strict mode passes.

- ESLint passes.

- Build succeeds without warnings.

Without a definition of done, the agent may stop at "looks okay."

---

#### 3. Design Language

Rather than only saying "Apple, Stripe, Microsoft," define concrete principles.

Examples:

- 8-point spacing system

- Maximum content width

- Typography scale

- Border radius

- Elevation rules

- Animation durations

- Motion easing

- Grid system

- Color tokens

- Responsive breakpoints

This makes the output much more consistent.

---

#### 4. Component Inventory

Specify every reusable component before page construction.

Examples:

- Header

- Mega Menu

- Footer

- Hero

- Product Hero

- CTA

- Timeline

- Principle Card

- Value Card

- Quote Block

- Statistic Card

- Feature Grid

- Product Comparison

- Pricing Table

- FAQ

- Testimonial

- Newsletter

- Contact Form

- Demo Form

The agent should build these once and reuse them.

---

#### 5. Folder Structure

Provide the desired architecture upfront.

For example:

apps/
website/

src/
 app/
 components/
 features/
 content/
 lib/
 styles/
 hooks/
 types/

This avoids ad hoc organization.

---

#### 6. Coding Standards

Require:

- Functional components

- TypeScript strict

- No any

- Server Components by default

- Client Components only when necessary

- Absolute imports

- Consistent naming

- No duplicated logic

---

#### 7. Motion System

Define motion globally instead of page by page.

For example:

- Fade

- Slide

- Stagger

- Hover

- Scroll reveal

Avoid arbitrary animations.

---

#### 8. SEO Standards

Every page should include:

- Title

- Description

- Canonical URL

- Open Graph

- Twitter Cards

- Structured Data

- Breadcrumbs where appropriate

- Sitemap integration

---

#### 9. Performance Budget

Set clear targets.

For example:

- Lighthouse Performance ≥95

- CLS <0.1

- LCP <2.5s

- JS bundle budget

- Image optimization

- Font loading strategy

This encourages performance-conscious implementation.

---

#### 10. Autonomous Workflow

This is the biggest addition I'd make.

Instead of asking Cursor to "build the website," define an execution pipeline such as:

1. Analyze the specification.

2. Produce an implementation plan.

3. Identify missing requirements.

4. Build the design system.

5. Build shared layouts.

6. Build reusable components.

7. Implement pages in priority order.

8. Test after each milestone.

9. Fix issues automatically.

10. Generate progress documentation.

11. Continue until every acceptance criterion is satisfied.

This creates a self-directed workflow rather than a one-shot generation.

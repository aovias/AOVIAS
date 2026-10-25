---
title: "AOVIAS Website Definition of Done (DoD)"
source: appflowy
workspace: AOVIAS Website
space: General
view_id: "0e2beb4e-20d1-42a6-98d8-6fc7caaec2b5"
exported_at: "2026-07-19"
---

# AOVIAS Website Definition of Done (DoD)
### Purpose

This document defines the mandatory acceptance criteria for every implementation within the AOVIAS Public Website.

A feature, component, page, or capability shall not be considered complete until all applicable Definition of Done requirements have been satisfied.

These requirements apply equally to:

- Autonomous AI Agents

- Human Developers

- Code Reviewers

- QA Engineers

- Future Contributors

---

## 1. General Principle

"Visually complete" is not considered "Done."

A page is only complete when it satisfies:

- Functional Quality

- Engineering Quality

- Accessibility

- Performance

- Maintainability

- Security

- Brand Consistency

- User Experience

---

## 2. Functional Completion

Every page shall:

- Render without runtime errors.

- Display all approved content.

- Support all intended user interactions.

- Contain no broken links.

- Contain no placeholder content.

- Contain no unfinished sections.

- Display correctly in supported browsers.

---

## 3. Responsive Design

Every page shall be fully responsive.

Minimum supported viewport widths:

- 320 px

- 375 px

- 390 px

- 414 px

- 768 px

- 1024 px

- 1280 px

- 1440 px

- 1920 px

- 2560 px

- 3840 px

Responsive verification includes:

- Navigation

- Typography

- Images

- Cards

- Tables

- Forms

- Interactive elements

- Layout spacing

No horizontal scrolling shall occur unless intentionally designed.

---

## 4. Accessibility

Every page shall satisfy:

- WCAG 2.2 AA compliance

- Keyboard navigation

- Visible focus indicators

- Semantic HTML

- Proper heading hierarchy

- Screen reader compatibility

- Sufficient color contrast

- Accessible form labels

- Accessible error messages

- Reduced motion support

Accessibility violations are release blockers.

---

## 5. Visual Quality

Every page shall:

- Match the approved design system.

- Maintain spacing consistency.

- Maintain typography consistency.

- Use approved color tokens.

- Use approved iconography.

- Follow motion guidelines.

- Respect light and dark themes.

Visual inconsistencies are defects.

---

## 6. Performance

Minimum Lighthouse targets:

CategoryMinimum ScorePerformance95Accessibility100Best Practices100SEO100

Core Web Vitals targets:

- LCP ≤ 2.5 s

- CLS ≤ 0.1

- INP ≤ 200 ms

Performance regressions shall be investigated before approval.

---

## 7. SEO

Every public page shall include:

- Unique page title

- Meta description

- Canonical URL

- Open Graph metadata

- Twitter Card metadata

- Structured data (JSON-LD) where appropriate

- Breadcrumb metadata (when applicable)

- Sitemap inclusion

- Robots directives

SEO implementation shall be complete before page approval.

---

## 8. Motion

Animations shall:

- Feel purposeful.

- Never distract.

- Support reduced-motion preferences.

- Maintain smooth performance.

- Avoid layout shifts.

- Preserve readability.

Motion is part of usability—not decoration.

---

## 9. Engineering Standards

Every implementation shall:

- Pass TypeScript strict mode.

- Pass ESLint.

- Produce zero console errors.

- Produce zero runtime warnings.

- Build successfully.

- Contain no dead code.

- Contain no duplicated logic.

- Follow project naming conventions.

---

## 10. Component Quality

Every new component shall:

- Be reusable.

- Be composable.

- Accept typed props.

- Support dark mode.

- Support responsive layouts.

- Include loading states where applicable.

- Include empty states where applicable.

- Include error states where applicable.

Components shall not contain page-specific assumptions unless explicitly intended.

---

## 11. Content Quality

Every page shall:

- Use approved messaging.

- Contain no marketing clichés.

- Contain no placeholder text.

- Contain no lorem ipsum.

- Maintain the AOVIAS tone of voice.

- Reinforce trust.

Every sentence should contribute to the user's understanding and confidence.

---

## 12. Forms

Every form shall:

- Validate inputs.

- Display helpful validation messages.

- Support keyboard navigation.

- Handle submission failures gracefully.

- Prevent duplicate submissions.

- Provide clear success feedback.

Forms are complete only when both success and failure paths have been verified.

---

## 13. Navigation

Navigation shall:

- Support keyboard interaction.

- Display active page state.

- Work across all screen sizes.

- Preserve accessibility.

- Avoid broken routes.

- Avoid dead-end journeys.

Navigation is considered part of every page's acceptance criteria.

---

## 14. Dark Mode

Every page shall:

- Support both themes.

- Maintain contrast.

- Preserve readability.

- Maintain brand consistency.

- Avoid visual glitches during theme transitions.

Theme switching shall not introduce layout shifts.

---

## 15. Browser Compatibility

Every page shall be verified in current stable versions of:

- Chrome

- Microsoft Edge

- Firefox

- Safari

No browser-specific defects shall remain unresolved without documented justification.

---

## 16. Security

Every implementation shall:

- Escape user-generated content.

- Prevent XSS vulnerabilities.

- Validate inputs.

- Avoid exposing sensitive information.

- Use secure defaults.

Security is considered complete only after implementation review.

---

## 17. Documentation

Every completed milestone shall include:

- Updated documentation.

- Updated component references.

- Updated route definitions.

- Updated dependency information.

- Updated implementation notes when architecture changes.

Documentation is part of the deliverable.

---

## 18. Automated Verification

Before approval, the following shall succeed:

- Production build

- Type checking

- Linting

- Unit tests (where applicable)

- End-to-end tests (where applicable)

- Lighthouse audit

- Accessibility audit

Failures shall block completion.

---

## 19. Review Checklist

Before marking work as complete, the agent shall confirm:

- Functionality verified

- Responsive design verified

- Accessibility verified

- Performance targets achieved

- SEO complete

- Dark mode verified

- Navigation verified

- Content approved

- Build successful

- Documentation updated

---

## 20. Completion Statement

A page is considered Done only when it satisfies all applicable requirements in this document.

Meeting the Definition of Done is mandatory and non-negotiable.

---

### Recommendation for the Website Master Specification

I would treat this as a living quality contract. As the website evolves—adding features like authentication, pricing, checkout, and customer portals—you can extend this document with additional criteria for those areas while keeping the same principle: nothing is "done" until it meets clearly defined, measurable standards. This discipline will be especially valuable when using autonomous AI agents, because it gives them objective targets instead of subjective notions like "looks complete."

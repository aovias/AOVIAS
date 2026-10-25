---
title: "AOVIAS Website Development Execution Rules"
source: appflowy
workspace: AOVIAS Website
space: General
view_id: "cb881e33-f6a2-4967-94bb-a8d19925f638"
exported_at: "2026-07-19"
---

# AOVIAS Website Development Execution Rules
## Purpose

This document establishes the mandatory engineering, architectural, design, and execution principles for developing the AOVIAS Public Website.

These rules are mandatory for:

- Autonomous AI Agents

- Human Developers

- Code Reviewers

- Future Contributors

The objective is to ensure the website evolves as a cohesive enterprise platform while preserving the philosophy, quality standards, and long-term vision of AOVIAS.

---

## 1. Core Principle

The AOVIAS website is not a marketing website.

It is the digital embodiment of the AOVIAS institution.

Every implementation decision shall strengthen:

- Trust

- Clarity

- Engineering Excellence

- Professionalism

- Longevity

- Accessibility

- Maintainability

No implementation shall prioritize visual novelty over long-term quality.

---

## 2. Autonomous Agent Responsibilities

The development agent shall behave as a Senior Principal Frontend Engineer.

The agent shall:

- Analyze requirements before implementation.

- Challenge ambiguous requirements.

- Identify missing dependencies.

- Recommend improvements when justified.

- Preserve architectural consistency.

- Maintain a production-ready codebase.

- Produce reusable solutions rather than shortcuts.

The agent shall never assume that speed is more important than quality.

---

## 3. Execution Principles

### 3.1 Incremental Development

Development shall proceed in small, verifiable milestones.

Each milestone must:

- Compile successfully.

- Pass linting.

- Pass type checking.

- Preserve existing functionality.

- Be independently reviewable.

Large, monolithic implementations are prohibited.

---

### 3.2 Reusable First

Reusable components shall always be implemented before page-specific implementations.

Examples:

- Buttons

- Cards

- Hero Sections

- CTA Blocks

- Navigation

- Forms

- Pricing Components

- Section Layouts

Pages shall assemble reusable components rather than create one-off implementations.

---

### 3.3 Composition over Duplication

Code duplication is prohibited.

When similar functionality exists:

- Extend the existing implementation.

- Generalize the component.

- Improve abstraction.

Never create a second implementation solely for convenience.

---

### 3.4 Architecture Preservation

Existing architecture shall not be replaced unless:

- A measurable improvement exists.

- Backward compatibility is maintained.

- Migration strategy is documented.

- Architectural consistency improves.

---

### 3.5 Production Quality

Every implementation shall be production-ready.

Temporary implementations are prohibited.

Examples:

- Placeholder styling

- Mock navigation

- Dummy architecture

- Hardcoded values intended for later replacement

---

## 4. Engineering Standards

Every change shall:

- Compile successfully.

- Pass TypeScript strict mode.

- Pass ESLint.

- Produce zero runtime errors.

- Maintain build integrity.

- Preserve responsive behavior.

Broken builds shall never be committed.

---

## 5. Design Consistency

Every page shall appear to belong to the same design system.

The following shall remain consistent:

- Typography

- Colors

- Spacing

- Grid

- Elevation

- Buttons

- Forms

- Cards

- Icons

- Motion

- Border Radius

- Shadows

Visual inconsistency is prohibited.

---

## 6. Accessibility

Accessibility is mandatory.

Every component shall support:

- Keyboard navigation

- Screen readers

- Semantic HTML

- Focus indicators

- ARIA where required

- Color contrast compliance

- Reduced motion preferences

Accessibility is a first-class requirement.

---

## 7. Performance

Performance shall be considered during implementation rather than after development.

Every implementation shall:

- Minimize JavaScript.

- Optimize images.

- Avoid unnecessary re-renders.

- Lazy load appropriate content.

- Minimize bundle size.

- Respect Core Web Vitals.

---

## 8. Responsive Design

Desktop-first or mobile-first is irrelevant.

Every component must function correctly across:

- Mobile

- Tablet

- Laptop

- Desktop

- Ultra-wide displays

Responsive behavior shall never be added later.

---

## 9. Component Ownership

Every component must have a single responsibility.

Large monolithic components shall be decomposed into smaller reusable units.

Components shall remain:

- Predictable

- Testable

- Reusable

- Self-contained

---

## 10. Content Integrity

The development agent shall never invent:

- Customer testimonials

- Statistics

- Awards

- Certifications

- Customer counts

- Revenue figures

- Employee numbers

- Marketing claims

All content shall originate from approved specifications.

---

## 11. Brand Protection

The website shall never resemble:

- Startup landing pages

- Cryptocurrency websites

- AI hype pages

- Consumer product websites

The visual language shall communicate:

- Confidence

- Stability

- Intelligence

- Professionalism

---

## 12. Technical Decision Making

When multiple valid implementations exist, choose the one that:

1. Improves maintainability.

2. Maximizes readability.

3. Reduces complexity.

4. Encourages reuse.

5. Improves performance.

6. Preserves architecture.

---

## 13. Documentation

Every significant implementation shall include:

- Clear naming

- Self-explanatory code

- Minimal but meaningful comments where necessary

- Updated documentation when behavior changes

Documentation shall evolve with implementation.

---

## 14. Error Handling

All user-facing operations shall:

- Handle failures gracefully.

- Provide meaningful feedback.

- Avoid exposing internal details.

- Preserve application stability.

Unhandled exceptions are unacceptable.

---

## 15. Quality Gates

A milestone is considered complete only when:

- Project builds successfully.

- Linting passes.

- Type checking passes.

- Accessibility requirements are satisfied.

- Responsive behavior is verified.

- Performance targets are maintained.

- Existing functionality remains intact.

- Documentation is updated.

---

## 16. Autonomous Development Workflow

For every assigned task, the agent shall execute the following sequence:

1. Analyze the requirement.

2. Review the existing architecture.

3. Identify reusable components.

4. Produce an implementation plan.

5. Implement incrementally.

6. Validate locally.

7. Perform self-review.

8. Resolve identified issues.

9. Update documentation.

10. Deliver a production-ready implementation.

No step shall be skipped without explicit justification.

---

## 17. Non-Negotiable Rules

The following actions are prohibited unless explicitly approved:

- Rewriting stable implementations.

- Introducing unnecessary dependencies.

- Duplicating existing functionality.

- Breaking architectural boundaries.

- Ignoring accessibility requirements.

- Ignoring responsive design.

- Committing broken builds.

- Leaving incomplete implementations.

- Using placeholder production content.

- Introducing visual inconsistency.

---

## 18. Guiding Question

Before implementing any change, the agent shall ask:

> "Does this implementation strengthen the perception that AOVIAS is a thoughtful, trustworthy, engineering-led institution whose technology serves society by strengthening the organizations people depend upon?"

If the answer is No, the implementation must be reconsidered.

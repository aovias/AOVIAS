# Sprint 12 — Analytics Platform Foundation

**Full reports:** `Sprint12B-0-Analytics-Stabilization-Report.md`, `Sprint12B-1-Analytics-Platform-Foundation-Report.md`, `Sprint12C-*`, `Sprint12D-*`

## Problem

Leadership lacked consolidated KPI trends; ad-hoc SQL and duplicate dashboard logic.

## Solution

- `AnalyticsDailySnapshot` entity and daily Hangfire job
- Analytics Hub Angular module (`/app/analytics`)
- Gateway `/analytics` proxy routes
- Copilot readiness design (12D) — input to Sprint 13

## Architecture

Read-only analytics CQRS ([ADR-008](../decisions/) implied in business rules).

## Business Impact

Single analytics entry in sidebar; trend views for readiness and operational metrics.

## Future Work

Unified KPI SSOT (TD-M05), BI export, Copilot analytics boundary documentation (TD-H04).

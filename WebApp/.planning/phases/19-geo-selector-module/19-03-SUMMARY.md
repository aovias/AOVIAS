---
plan: 19-03
phase: 19
title: Integrate GeoSelector into onboarding/signup flow (create + edit)
date: 2026-04-02
---

## What shipped

- **Hospital model updated** to include standardized geo fields: `countryCode`, `stateCode`, `cityName`.
- **Signup (Hospital step) integrated** with `<app-geo-selector formControlName="geo">`.
- **Create + edit flows supported**:
  - On load/rehydrate, existing stored hospital geo fields are mapped into `geo` CVA value.
  - On step save (next/back), `geo` is mapped into the minimal backend contract fields.
- **Payload contract**: onboarding request now carries only `{ countryCode, stateCode, cityName }` (plus existing hospital fields) under `hospital`.

## Files changed

- `src/app/features/onboarding/models/hospital.model.ts`
- `src/app/signup/signup.ts`
- `src/app/signup/signup.html`

## Verification

- `npm run build` ✅

## Commits

- `feat(19-03): integrate geo selector into signup hospital step` — `ae08e0a`


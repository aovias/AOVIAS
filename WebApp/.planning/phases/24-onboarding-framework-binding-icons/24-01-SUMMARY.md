---
phase: 24
plan: 24-01
status: complete
---

# Summary 24-01: Framework cards — binding, platformId, Lucide icons

## What changed

- Added **`@ng-icons/core`** and **`@ng-icons/lucide`** dependencies.
- New **`FrameworkCardIconComponent`** (`app-framework-card-icon`): `provideIcons` with a curated Lucide set, whitelist map from API `iconKey` → registered icon name, optional pass-through for registered `lucide*` names, default **`lucideShield`**.
- **`FrameworkLookup`**: canonical **`platformId`**; **`authority`** built from API `authority` first, then **`code`** when authority missing (documented in model + normalizer); optional **`iconKey`**.
- **`CommonLookupService.normalizeFramework`**: reads `platformId` / `frameworkId`, `iconKey` / `icon`, authority/code as above.
- **`FrameworkSelectorStepComponent`**: **`localSelectedIds`** copy synced from `@Input() selectedFrameworks` via **`ngOnChanges`**; **`trackByPlatformId`**; **`FrameworkCardIconComponent`** in template; subtitle only if non-empty; emits **`{ platformIds, displayNames }`**; **`OnPush`** + **`markForCheck`** after async/toggle; no UUIDs in template text; display name fallback **`'Framework'`** if row missing.
- **`AiSetupAssistantComponent`**: **`handleFrameworksNext`** uses **`platformIds`** in payload type.

## Files touched

- `package.json`, `package-lock.json`
- `src/app/shared/models/lookup.models.ts`
- `src/app/shared/services/common-lookup.service.ts`
- `src/app/shared/ui/framework-card-icon/framework-card-icon.component.ts` (new)
- `src/app/features/onboarding/components/framework-selector-step.component.ts`
- `src/app/features/onboarding/pages/ai-setup-assistant.component.ts`
- `src/app/features/onboarding/models/onboarding-state.model.ts`
- `src/app/features/onboarding/models/hospital.model.ts`

## Verification

- `npx ng build --configuration=development` succeeds.

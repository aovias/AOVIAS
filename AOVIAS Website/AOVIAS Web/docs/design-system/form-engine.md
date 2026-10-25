# Form Engine API

Import engine: `import { FormEngine, contactFormConfig, … } from '@/features/forms'`  
Import bindings: `import { ContactForm, … } from '@/components/forms'`

Showcase: `/internal/design-system/forms`

Forms are **configuration + Zod schema**, rendered by one shared engine. No backend, CRM, or CAPTCHA in Sprint 2.4.

## Architecture

```text
features/forms/
  engine/       FormEngine, FormConfig types
  schemas/      Contact, Demo, Newsletter, Enterprise, Partner
  fields/       Field shell + FormFieldControl
  validators/   Shared Zod builders + messages
  adapters/     zodResolver, mock/API submit shaping
  hooks/        useFormEngine
  states/       FormSuccess, FormErrorBanner

components/forms/
  *-form.tsx    Thin bindings (config → FormEngine)
```

## FormConfig

```ts
type FormConfig<TValues> = {
  id: string;
  ariaLabel: string;
  schema: ZodType<TValues>;
  fields: FormFieldConfig[];
  sections?: FormSectionConfig[];
  defaultValues?: DefaultValues<TValues>;
  submitLabel?: string;
  success?: FormSuccessConfig;
  columns?: 1 | 2;
};
```

Field types: `text` | `email` | `tel` | `textarea` | `select` | `checkbox` | `radio` | `switch` | `hidden`

## Validation Framework

Shared builders in `@/features/forms/validators`:

| Builder | Purpose |
| --- | --- |
| `requiredString` / `lengthString` | Required + length |
| `emailField` / `optionalEmailField` | Email |
| `phoneField` / `optionalPhoneField` | Phone pattern |
| `optionalUrlField` | URL |
| `patternString` | Custom regex |
| `consentField` | Must be `true` |
| `selectField` | Enum options |
| `withCustom` | Custom validator wrapper |

Cross-field example (Demo Request): phone required when preferred contact is `phone` (`superRefine`).

Messages: `validationMessages` — single source of copy.

## Submission contract

```ts
type FormSubmitResult =
  | { ok: true; message?: string; followUp?: string }
  | { ok: false; message: string; retryable?: boolean; fieldErrors?: Record<string, string> };

onSubmit: (values) => Promise<FormSubmitResult>
```

Use `createMockSubmitHandler()` for QA. Replace with CRM / Identity adapters later — UI stays the same.

## States

| Status | Behaviour |
| --- | --- |
| idle | Default interactive form |
| submitting | Button loading + duplicate-submit guard |
| success | `FormSuccess` (illustration placeholder, follow-up, reset) |
| error | Field errors via Zod/RHF + `FormErrorBanner` retry |

Control states compose primitives: focus, hover, disabled, read-only, `aria-invalid` error styling.

## Accessibility

- Native `<form>`, labels, fieldsets/legends for radios
- `aria-invalid` + `aria-describedby` for description/error
- Error text announced with `role="alert"` / assertive form alert
- Keyboard operable controls; Switch uses `role="switch"`
- `noValidate` — engine owns validation UX

## Motion

- Success / error banners use Motion Registry `fadeIn` via `MotionReveal`
- No inline Framer variants

## Theme & responsive

- Token surfaces only (`border-border`, `text-error`, success/error alerts)
- Light/dark via Theme System
- 1–2 column grids from `md`; full-width fields via `colSpan: 2`

## Usage

```tsx
import { ContactForm } from '@/components/forms';
import { createMockSubmitHandler } from '@/features/forms';

<ContactForm
  onSubmit={createMockSubmitHandler({
    message: 'Thank you for contacting AOVIAS.',
  })}
/>
```

```tsx
import { FormEngine, partnerFormConfig } from '@/features/forms';

<FormEngine
  config={partnerFormConfig}
  onSubmit={async (values) => {
    // Future: CRM adapter
    return { ok: true, message: 'Received.' };
  }}
/>
```

# Form Engine

Configuration-driven enterprise form architecture for the AOVIAS public website.

Import: `import { FormEngine, contactFormConfig, … } from '@/features/forms'`

## Layout

```text
features/forms/
  engine/       # FormEngine + shared types
  schemas/      # Contact, Demo, Newsletter, Enterprise, Partner configs
  fields/       # Field shell + typed controls
  validators/   # Shared Zod rules + messages
  adapters/     # Submit shaping + zodResolver
  hooks/        # useFormEngine lifecycle
  states/       # Success / error presentation
```

Each form is a `FormConfig` (fields + sections + Zod schema) rendered by `FormEngine`.

Showcase: `/internal/design-system/forms`

See `docs/design-system/form-engine.md`.

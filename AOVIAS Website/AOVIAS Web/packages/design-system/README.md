# `@aovias/design-system`

Shared AOVIAS design tokens, CSS variables, light/dark themes, and theme runtime.

## Consume in an app

```ts
import '@aovias/design-system/css';
import { ThemeProvider, ThemeToggle, tokens } from '@aovias/design-system';
```

Use Tailwind semantic utilities (`bg-background`, `text-foreground`, `p-4`, `rounded-md`) — never hardcode visual values.

# Forms

Page-facing form components. Each binds a Form Engine config from `@/features/forms`.

```tsx
import { ContactForm } from '@/components/forms';
import { createMockSubmitHandler } from '@/features/forms';

<ContactForm onSubmit={createMockSubmitHandler()} />
```

Do not add page-specific fields here — extend schemas/configs in `@/features/forms`.

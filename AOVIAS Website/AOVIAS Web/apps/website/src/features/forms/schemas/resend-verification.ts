import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import { emailField } from '@/features/forms/validators';

export const resendVerificationFormSchema = z.object({
  email: emailField(),
});

export type ResendVerificationFormValues = z.infer<
  typeof resendVerificationFormSchema
>;

export const resendVerificationFormConfig: FormConfig<ResendVerificationFormValues> =
  {
    id: 'resend-verification-form',
    ariaLabel: 'Resend verification form',
    schema: resendVerificationFormSchema,
    columns: 1,
    submitLabel: 'Resend verification',
    submittingLabel: 'Sending…',
    success: {
      title: 'Verification sent',
      message:
        'If that address still needs verification, a new message was sent.',
    },
    defaultValues: {
      email: '',
    },
    fields: [
      {
        name: 'email',
        type: 'email',
        label: 'Work email',
        required: true,
        autoComplete: 'email',
      },
    ],
  };

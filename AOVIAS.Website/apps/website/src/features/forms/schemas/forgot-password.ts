import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import { emailField } from '@/features/forms/validators';

export const forgotPasswordFormSchema = z.object({
  email: emailField(),
});

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordFormSchema>;

export const forgotPasswordFormConfig: FormConfig<ForgotPasswordFormValues> = {
  id: 'forgot-password-form',
  ariaLabel: 'Forgot password form',
  schema: forgotPasswordFormSchema,
  columns: 1,
  submitLabel: 'Send reset link',
  submittingLabel: 'Sending…',
  success: {
    title: 'Check your email',
    message:
      'If an account exists for that address, password reset instructions were sent.',
    followUp: 'The link expires for your security. You can request another if needed.',
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
      description: 'We will email reset instructions if the account exists.',
    },
  ],
};

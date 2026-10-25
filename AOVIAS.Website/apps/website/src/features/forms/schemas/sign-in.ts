import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import { emailField, passwordField } from '@/features/forms/validators';

export const signInFormSchema = z.object({
  email: emailField(),
  password: passwordField(),
  rememberMe: z.boolean().optional().default(false),
});

export type SignInFormValues = z.infer<typeof signInFormSchema>;

export const signInFormConfig: FormConfig<SignInFormValues> = {
  id: 'sign-in-form',
  ariaLabel: 'Sign in form',
  schema: signInFormSchema,
  columns: 1,
  submitLabel: 'Sign in',
  submittingLabel: 'Signing in…',
  success: {
    title: 'Signed in',
    message: 'Welcome back to AOVIAS.',
    followUp: 'You can continue to your workspace when Identity redirects.',
  },
  defaultValues: {
    email: '',
    password: '',
    rememberMe: false,
  },
  fields: [
    {
      name: 'email',
      type: 'email',
      label: 'Work email',
      required: true,
      autoComplete: 'email',
    },
    {
      name: 'password',
      type: 'password',
      label: 'Password',
      required: true,
      autoComplete: 'current-password',
    },
    {
      name: 'rememberMe',
      type: 'checkbox',
      label: 'Remember me on this device',
    },
  ],
};

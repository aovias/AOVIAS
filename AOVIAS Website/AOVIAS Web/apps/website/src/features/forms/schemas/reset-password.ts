import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import { validationMessages } from '@/features/forms/validators/messages';
import { passwordField } from '@/features/forms/validators';

export const resetPasswordFormSchema = z
  .object({
    password: passwordField(),
    confirmPassword: passwordField(),
  })
  .superRefine((data, ctx) => {
    if (data.password !== data.confirmPassword) {
      ctx.addIssue({
        code: 'custom',
        message: validationMessages.passwordMismatch,
        path: ['confirmPassword'],
      });
    }
  });

export type ResetPasswordFormValues = z.infer<typeof resetPasswordFormSchema>;

export const resetPasswordFormConfig: FormConfig<ResetPasswordFormValues> = {
  id: 'reset-password-form',
  ariaLabel: 'Reset password form',
  schema: resetPasswordFormSchema,
  columns: 1,
  submitLabel: 'Update password',
  submittingLabel: 'Updating…',
  success: {
    title: 'Password updated',
    message: 'You can now sign in with your new password.',
    followUp: 'Use a unique password for institutional accounts.',
  },
  defaultValues: {
    password: '',
    confirmPassword: '',
  },
  fields: [
    {
      name: 'password',
      type: 'password',
      label: 'New password',
      required: true,
      autoComplete: 'new-password',
      description: 'At least 8 characters with a letter and a number.',
    },
    {
      name: 'confirmPassword',
      type: 'password',
      label: 'Confirm new password',
      required: true,
      autoComplete: 'new-password',
    },
  ],
};

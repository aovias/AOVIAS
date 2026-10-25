import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import { validationMessages } from '@/features/forms/validators/messages';
import {
  consentField,
  emailField,
  lengthString,
  passwordField,
} from '@/features/forms/validators';

export const signUpFormSchema = z
  .object({
    name: lengthString(2, 100),
    email: emailField(),
    password: passwordField(),
    confirmPassword: passwordField(),
    acceptedTerms: consentField(validationMessages.terms),
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

export type SignUpFormValues = z.infer<typeof signUpFormSchema>;

export const signUpFormConfig: FormConfig<SignUpFormValues> = {
  id: 'sign-up-form',
  ariaLabel: 'Create account form',
  schema: signUpFormSchema,
  columns: 1,
  submitLabel: 'Create account',
  submittingLabel: 'Creating account…',
  success: {
    title: 'Account created',
    message: 'Check your email to verify your address before signing in.',
    followUp: 'Verification keeps institutional accounts secure.',
  },
  defaultValues: {
    name: '',
    email: '',
    password: '',
    confirmPassword: '',
    acceptedTerms: false,
  },
  fields: [
    {
      name: 'name',
      type: 'text',
      label: 'Full name',
      required: true,
      autoComplete: 'name',
    },
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
      autoComplete: 'new-password',
      description: 'At least 8 characters with a letter and a number.',
    },
    {
      name: 'confirmPassword',
      type: 'password',
      label: 'Confirm password',
      required: true,
      autoComplete: 'new-password',
    },
    {
      name: 'acceptedTerms',
      type: 'checkbox',
      label: 'I accept the Terms of Service and Privacy Policy',
      description: 'See /legal/terms and /legal/privacy for the full documents.',
      required: true,
    },
  ],
};

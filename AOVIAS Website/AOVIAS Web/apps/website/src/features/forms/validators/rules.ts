import { z } from 'zod';

import { validationMessages } from './messages';

/** E.164-friendly phone: optional +, digits, spaces, dashes, parentheses. */
const PHONE_PATTERN = /^[+]?[\d\s().-]{7,22}$/;

/**
 * Reusable Zod field builders for the Form Engine.
 * Prefer these over ad-hoc rules in form schemas.
 */
export function requiredString(message = validationMessages.required) {
  return z.string().trim().min(1, message);
}

export function emailField(message = validationMessages.email) {
  return requiredString().pipe(z.email(message));
}

export function optionalEmailField(message = validationMessages.email) {
  return z.union([z.literal(''), z.email(message)]);
}

export function phoneField(message = validationMessages.phone) {
  return requiredString().regex(PHONE_PATTERN, message);
}

export function optionalPhoneField(message = validationMessages.phone) {
  return z.union([
    z.literal(''),
    z.string().trim().regex(PHONE_PATTERN, message),
  ]);
}

export function optionalUrlField(message = validationMessages.url) {
  return z.union([z.literal(''), z.url(message)]);
}

export function lengthString(min: number, max: number) {
  return requiredString()
    .min(min, validationMessages.minLength(min))
    .max(max, validationMessages.maxLength(max));
}

export function optionalLengthString(max: number, min = 0) {
  return z
    .string()
    .trim()
    .max(max, validationMessages.maxLength(max))
    .refine((value) => value.length === 0 || value.length >= min, {
      message: validationMessages.minLength(min),
    });
}

export function patternString(pattern: RegExp, message = validationMessages.pattern) {
  return requiredString().regex(pattern, message);
}

export function consentField(message: string = validationMessages.consent) {
  return z.boolean().refine((value) => value === true, { message });
}

/** Password for Identity forms — complexity only; hashing stays on Identity Platform. */
export function passwordField(
  message = validationMessages.passwordComplexity,
) {
  return requiredString()
    .min(8, validationMessages.minLength(8))
    .max(128, validationMessages.maxLength(128))
    .regex(/^(?=.*[A-Za-z])(?=.*\d).+$/, message);
}

export function optionalBooleanField() {
  return z.boolean().optional().default(false);
}

export function selectField<T extends string>(
  values: readonly [T, ...T[]],
  message = validationMessages.selectOption,
) {
  return z.enum(values, { error: message });
}

/**
 * Attach a custom validator to an existing schema.
 */
export function withCustom<T extends z.ZodType>(
  schema: T,
  validate: (value: z.infer<T>) => string | null,
) {
  return schema.superRefine((value, ctx) => {
    const message = validate(value as z.infer<T>);
    if (message) {
      ctx.addIssue({ code: 'custom', message });
    }
  });
}

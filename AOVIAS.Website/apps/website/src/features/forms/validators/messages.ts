/**
 * Shared validation messages — keep copy consistent across all forms.
 */
export const validationMessages = {
  required: 'This field is required.',
  email: 'Enter a valid work email address.',
  phone: 'Enter a valid phone number, including country code if applicable.',
  url: 'Enter a valid URL, including https://.',
  minLength: (min: number) => `Enter at least ${min} characters.`,
  maxLength: (max: number) => `Use at most ${max} characters.`,
  pattern: 'Enter a value in the expected format.',
  consent: 'Consent is required to continue.',
  selectOption: 'Select an option to continue.',
  passwordComplexity:
    'Use at least 8 characters with a letter and a number.',
  passwordMismatch: 'Passwords do not match.',
  terms: 'Accept the terms to continue.',
  crossFieldPhoneRequired:
    'Enter a phone number when phone is your preferred contact method.',
} as const;

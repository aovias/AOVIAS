import type { DefaultValues, FieldValues } from 'react-hook-form';
import type { ZodType } from 'zod';

export type FormFieldType =
  | 'text'
  | 'email'
  | 'tel'
  | 'password'
  | 'textarea'
  | 'select'
  | 'checkbox'
  | 'radio'
  | 'switch'
  | 'hidden'
  | 'honeypot';

export type FormFieldOption = {
  value: string;
  label: string;
};

export type FormFieldConfig = {
  name: string;
  type: FormFieldType;
  label?: string;
  description?: string;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
  readOnly?: boolean;
  autoComplete?: string;
  options?: readonly FormFieldOption[];
  /** Grid column span within a two-column section. */
  colSpan?: 1 | 2;
  /** Groups fields under a section heading. */
  section?: string;
  /** Hidden field static value. */
  value?: string;
};

export type FormSectionConfig = {
  id: string;
  title?: string;
  description?: string;
};

export type FormSuccessConfig = {
  title?: string;
  message?: string;
  followUp?: string;
  showIllustration?: boolean;
};

export type FormStatus =
  | 'idle'
  | 'submitting'
  | 'success'
  | 'error'
  | 'disabled';

export type FormSubmitSuccess = {
  ok: true;
  message?: string;
  followUp?: string;
};

export type FormSubmitFailure = {
  ok: false;
  message: string;
  retryable?: boolean;
  fieldErrors?: Record<string, string>;
};

export type FormSubmitResult = FormSubmitSuccess | FormSubmitFailure;

export type FormSubmitHandler<TValues extends FieldValues> = (
  values: TValues,
) => Promise<FormSubmitResult>;

/**
 * Configuration that drives the Form Engine — schemas + field layout.
 * Concrete forms are configs, not bespoke React trees.
 */
export type FormConfig<TValues extends FieldValues = FieldValues> = {
  id: string;
  /** Accessible form name for screen readers. */
  ariaLabel: string;
  schema: ZodType<TValues>;
  fields: readonly FormFieldConfig[];
  sections?: readonly FormSectionConfig[];
  defaultValues?: DefaultValues<TValues>;
  submitLabel?: string;
  submittingLabel?: string;
  success?: FormSuccessConfig;
  /** Two-column layout from `md` breakpoint. */
  columns?: 1 | 2;
};

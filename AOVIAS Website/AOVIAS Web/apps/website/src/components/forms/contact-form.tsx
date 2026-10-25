'use client';

import {
  contactFormConfig,
  FormEngine,
  type FormSubmitHandler,
  type ContactFormValues,
} from '@/features/forms';

export type ContactFormProps = {
  onSubmit: FormSubmitHandler<ContactFormValues>;
  className?: string;
  disabled?: boolean;
};

/** Contact form — thin binding over FormEngine + contactFormConfig. */
export function ContactForm(props: ContactFormProps) {
  return <FormEngine config={contactFormConfig} {...props} />;
}

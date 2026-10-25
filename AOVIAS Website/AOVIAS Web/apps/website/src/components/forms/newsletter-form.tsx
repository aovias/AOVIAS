'use client';

import {
  FormEngine,
  newsletterFormConfig,
  type FormSubmitHandler,
  type NewsletterFormValues,
} from '@/features/forms';

export type NewsletterFormProps = {
  onSubmit: FormSubmitHandler<NewsletterFormValues>;
  className?: string;
  disabled?: boolean;
};

export function NewsletterForm(props: NewsletterFormProps) {
  return <FormEngine config={newsletterFormConfig} {...props} />;
}

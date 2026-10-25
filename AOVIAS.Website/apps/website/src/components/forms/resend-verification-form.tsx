'use client';

import {
  FormEngine,
  resendVerificationFormConfig,
  type FormSubmitHandler,
  type ResendVerificationFormValues,
} from '@/features/forms';

export type ResendVerificationFormProps = {
  onSubmit: FormSubmitHandler<ResendVerificationFormValues>;
  className?: string;
  disabled?: boolean;
};

export function ResendVerificationForm(props: ResendVerificationFormProps) {
  return <FormEngine config={resendVerificationFormConfig} {...props} />;
}

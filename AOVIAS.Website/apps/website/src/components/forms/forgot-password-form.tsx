'use client';

import {
  FormEngine,
  forgotPasswordFormConfig,
  type ForgotPasswordFormValues,
  type FormSubmitHandler,
} from '@/features/forms';

export type ForgotPasswordFormProps = {
  onSubmit: FormSubmitHandler<ForgotPasswordFormValues>;
  className?: string;
  disabled?: boolean;
};

export function ForgotPasswordForm(props: ForgotPasswordFormProps) {
  return <FormEngine config={forgotPasswordFormConfig} {...props} />;
}

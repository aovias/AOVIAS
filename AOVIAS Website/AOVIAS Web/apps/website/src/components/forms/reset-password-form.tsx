'use client';

import {
  FormEngine,
  resetPasswordFormConfig,
  type FormSubmitHandler,
  type ResetPasswordFormValues,
} from '@/features/forms';

export type ResetPasswordFormProps = {
  onSubmit: FormSubmitHandler<ResetPasswordFormValues>;
  className?: string;
  disabled?: boolean;
};

export function ResetPasswordForm(props: ResetPasswordFormProps) {
  return <FormEngine config={resetPasswordFormConfig} {...props} />;
}

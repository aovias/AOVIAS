'use client';

import {
  FormEngine,
  signInFormConfig,
  type FormSubmitHandler,
  type SignInFormValues,
} from '@/features/forms';

export type SignInFormProps = {
  onSubmit: FormSubmitHandler<SignInFormValues>;
  className?: string;
  disabled?: boolean;
};

export function SignInForm(props: SignInFormProps) {
  return <FormEngine config={signInFormConfig} {...props} />;
}

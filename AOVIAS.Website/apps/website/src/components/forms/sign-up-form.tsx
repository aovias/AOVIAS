'use client';

import {
  FormEngine,
  signUpFormConfig,
  type FormSubmitHandler,
  type SignUpFormValues,
} from '@/features/forms';

export type SignUpFormProps = {
  onSubmit: FormSubmitHandler<SignUpFormValues>;
  className?: string;
  disabled?: boolean;
};

export function SignUpForm(props: SignUpFormProps) {
  return <FormEngine config={signUpFormConfig} {...props} />;
}

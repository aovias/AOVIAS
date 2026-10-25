'use client';

import {
  checkoutConfirmFormConfig,
  FormEngine,
  type CheckoutConfirmFormValues,
  type FormSubmitHandler,
} from '@/features/forms';

export type CheckoutConfirmFormProps = {
  onSubmit: FormSubmitHandler<CheckoutConfirmFormValues>;
  className?: string;
  disabled?: boolean;
};

export function CheckoutConfirmForm(props: CheckoutConfirmFormProps) {
  return <FormEngine config={checkoutConfirmFormConfig} {...props} />;
}

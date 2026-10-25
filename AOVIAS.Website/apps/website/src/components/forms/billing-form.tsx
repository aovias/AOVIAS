'use client';

import {
  billingFormConfig,
  FormEngine,
  type BillingFormSchemaValues,
  type FormSubmitHandler,
} from '@/features/forms';

export type BillingFormProps = {
  onSubmit: FormSubmitHandler<BillingFormSchemaValues>;
  className?: string;
  disabled?: boolean;
};

export function BillingForm(props: BillingFormProps) {
  return <FormEngine config={billingFormConfig} {...props} />;
}

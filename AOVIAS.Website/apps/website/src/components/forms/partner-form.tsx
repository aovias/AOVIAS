'use client';

import {
  FormEngine,
  partnerFormConfig,
  type FormSubmitHandler,
  type PartnerFormValues,
} from '@/features/forms';

export type PartnerFormProps = {
  onSubmit: FormSubmitHandler<PartnerFormValues>;
  className?: string;
  disabled?: boolean;
};

export function PartnerForm(props: PartnerFormProps) {
  return <FormEngine config={partnerFormConfig} {...props} />;
}

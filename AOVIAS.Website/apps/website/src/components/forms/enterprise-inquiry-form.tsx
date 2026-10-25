'use client';

import {
  enterpriseInquiryFormConfig,
  FormEngine,
  type EnterpriseInquiryFormValues,
  type FormSubmitHandler,
} from '@/features/forms';

export type EnterpriseInquiryFormProps = {
  onSubmit: FormSubmitHandler<EnterpriseInquiryFormValues>;
  className?: string;
  disabled?: boolean;
};

export function EnterpriseInquiryForm(props: EnterpriseInquiryFormProps) {
  return <FormEngine config={enterpriseInquiryFormConfig} {...props} />;
}

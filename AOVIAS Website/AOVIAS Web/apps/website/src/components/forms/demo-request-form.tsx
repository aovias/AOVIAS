'use client';

import {
  demoRequestFormConfig,
  FormEngine,
  type DemoRequestFormValues,
  type FormSubmitHandler,
} from '@/features/forms';

export type DemoRequestFormProps = {
  onSubmit: FormSubmitHandler<DemoRequestFormValues>;
  className?: string;
  disabled?: boolean;
};

export function DemoRequestForm(props: DemoRequestFormProps) {
  return <FormEngine config={demoRequestFormConfig} {...props} />;
}

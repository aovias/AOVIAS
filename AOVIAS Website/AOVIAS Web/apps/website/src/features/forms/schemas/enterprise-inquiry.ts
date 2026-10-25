import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import {
  emailField,
  lengthString,
  optionalLengthString,
  optionalPhoneField,
  selectField,
} from '@/features/forms/validators';

const companySizeValues = [
  '1-50',
  '51-200',
  '201-1000',
  '1000+',
] as const;

export const enterpriseInquiryFormSchema = z.object({
  organization: lengthString(2, 120),
  industry: optionalLengthString(80),
  companySize: selectField(companySizeValues),
  contactName: lengthString(2, 100),
  email: emailField(),
  phone: optionalPhoneField(),
  requirements: lengthString(20, 2500),
  projectSummary: lengthString(20, 2500),
});

export type EnterpriseInquiryFormValues = z.infer<
  typeof enterpriseInquiryFormSchema
>;

export const enterpriseInquiryFormConfig: FormConfig<EnterpriseInquiryFormValues> =
  {
    id: 'enterprise-inquiry-form',
    ariaLabel: 'Enterprise inquiry form',
    schema: enterpriseInquiryFormSchema,
    columns: 2,
    submitLabel: 'Submit inquiry',
    submittingLabel: 'Submitting…',
    success: {
      title: 'Inquiry received',
      message: 'Thank you for your enterprise inquiry.',
      followUp: 'We typically respond within one business day.',
      showIllustration: false,
    },
    sections: [
      { id: 'organization', description: 'Organization information' },
      { id: 'contact', description: 'Contact details' },
      { id: 'project', description: 'Business requirements' },
    ],
    defaultValues: {
      organization: '',
      industry: '',
      contactName: '',
      email: '',
      phone: '',
      requirements: '',
      projectSummary: '',
    },
    fields: [
      {
        name: 'organization',
        type: 'text',
        label: 'Organization',
        required: true,
        autoComplete: 'organization',
        section: 'organization',
      },
      {
        name: 'industry',
        type: 'text',
        label: 'Industry',
        section: 'organization',
      },
      {
        name: 'companySize',
        type: 'select',
        label: 'Company size',
        required: true,
        placeholder: 'Select company size',
        section: 'organization',
        colSpan: 2,
        options: [
          { value: '1-50', label: '1–50' },
          { value: '51-200', label: '51–200' },
          { value: '201-1000', label: '201–1,000' },
          { value: '1000+', label: '1,000+' },
        ],
      },
      {
        name: 'contactName',
        type: 'text',
        label: 'Contact name',
        required: true,
        autoComplete: 'name',
        section: 'contact',
      },
      {
        name: 'email',
        type: 'email',
        label: 'Work email',
        required: true,
        autoComplete: 'email',
        section: 'contact',
      },
      {
        name: 'phone',
        type: 'tel',
        label: 'Phone',
        autoComplete: 'tel',
        section: 'contact',
        colSpan: 2,
      },
      {
        name: 'requirements',
        type: 'textarea',
        label: 'Business requirements',
        required: true,
        description: 'Outcomes, constraints, and stakeholders.',
        section: 'project',
        colSpan: 2,
      },
      {
        name: 'projectSummary',
        type: 'textarea',
        label: 'Project summary',
        required: true,
        section: 'project',
        colSpan: 2,
      },
    ],
  };

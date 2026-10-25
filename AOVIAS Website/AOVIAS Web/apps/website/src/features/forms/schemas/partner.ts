import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import {
  emailField,
  lengthString,
  optionalPhoneField,
  optionalUrlField,
  selectField,
} from '@/features/forms/validators';

const partnershipTypeValues = [
  'technology',
  'reseller',
  'systems-integrator',
  'advisory',
  'other',
] as const;

export const partnerFormSchema = z.object({
  organization: lengthString(2, 120),
  partnershipType: selectField(partnershipTypeValues),
  contactName: lengthString(2, 100),
  email: emailField(),
  phone: optionalPhoneField(),
  website: optionalUrlField(),
  details: lengthString(20, 2500),
  source: z.string().optional(),
});

export type PartnerFormValues = z.infer<typeof partnerFormSchema>;

export const partnerFormConfig: FormConfig<PartnerFormValues> = {
  id: 'partner-form',
  ariaLabel: 'Partner inquiry form',
  schema: partnerFormSchema,
  columns: 2,
  submitLabel: 'Submit partnership inquiry',
  submittingLabel: 'Submitting…',
  success: {
    title: 'Partnership inquiry received',
    message: 'Thank you for your interest in partnering with AOVIAS.',
    followUp: 'We typically respond within one business day.',
    showIllustration: false,
  },
  sections: [
    { id: 'organization', description: 'Organization' },
    { id: 'contact', description: 'Contact information' },
    { id: 'details', description: 'Additional details' },
  ],
  defaultValues: {
    organization: '',
    contactName: '',
    email: '',
    phone: '',
    website: '',
    details: '',
    source: 'website',
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
      name: 'partnershipType',
      type: 'select',
      label: 'Partnership type',
      required: true,
      placeholder: 'Select partnership type',
      section: 'organization',
      options: [
        { value: 'technology', label: 'Technology partner' },
        { value: 'reseller', label: 'Reseller' },
        { value: 'systems-integrator', label: 'Systems integrator' },
        { value: 'advisory', label: 'Advisory' },
        { value: 'other', label: 'Other' },
      ],
    },
    {
      name: 'website',
      type: 'text',
      label: 'Website',
      placeholder: 'https://',
      autoComplete: 'url',
      section: 'organization',
      colSpan: 2,
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
      name: 'details',
      type: 'textarea',
      label: 'Business overview',
      required: true,
      description: 'Describe the partnership opportunity, capabilities, and fit.',
      section: 'details',
      colSpan: 2,
    },
    {
      name: 'source',
      type: 'hidden',
      value: 'website',
      section: 'details',
    },
  ],
};

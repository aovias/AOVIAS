import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import { validationMessages } from '@/features/forms/validators/messages';
import {
  emailField,
  lengthString,
  optionalLengthString,
  optionalPhoneField,
  selectField,
} from '@/features/forms/validators';

const productInterestValues = [
  'aovias-command',
  'platform',
  'integration',
  'other',
] as const;

const preferredContactValues = ['email', 'phone', 'either'] as const;

const preferredScheduleValues = [
  'within-2-weeks',
  'this-month',
  'next-quarter',
  'flexible',
] as const;

export const demoRequestFormSchema = z
  .object({
    firstName: lengthString(1, 60),
    lastName: lengthString(1, 60),
    email: emailField(),
    phone: optionalPhoneField(),
    organization: lengthString(2, 120),
    jobTitle: optionalLengthString(80),
    productInterest: selectField(productInterestValues),
    preferredSchedule: selectField(preferredScheduleValues),
    preferredContact: selectField(preferredContactValues),
    notes: optionalLengthString(1500),
  })
  .superRefine((data, ctx) => {
    if (data.preferredContact === 'phone' && !data.phone.trim()) {
      ctx.addIssue({
        code: 'custom',
        message: validationMessages.crossFieldPhoneRequired,
        path: ['phone'],
      });
    }
  });

export type DemoRequestFormValues = z.infer<typeof demoRequestFormSchema>;

export const demoRequestFormConfig: FormConfig<DemoRequestFormValues> = {
  id: 'demo-request-form',
  ariaLabel: 'Demo request form',
  schema: demoRequestFormSchema,
  columns: 2,
  submitLabel: 'Request a demo',
  submittingLabel: 'Submitting…',
  success: {
    title: 'Demo request received',
    message:
      'Thank you. We will follow up to schedule a focused demonstration with your team.',
    followUp: 'We typically respond within one business day.',
    showIllustration: false,
  },
  sections: [
    { id: 'contact', description: 'Contact information' },
    { id: 'organization', description: 'Organization details' },
    { id: 'interest', description: 'Product interest' },
  ],
  defaultValues: {
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    organization: '',
    jobTitle: '',
    notes: '',
  },
  fields: [
    {
      name: 'firstName',
      type: 'text',
      label: 'First name',
      required: true,
      autoComplete: 'given-name',
      section: 'contact',
    },
    {
      name: 'lastName',
      type: 'text',
      label: 'Last name',
      required: true,
      autoComplete: 'family-name',
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
      description: 'Required when phone is the preferred contact method.',
      section: 'contact',
    },
    {
      name: 'organization',
      type: 'text',
      label: 'Organization',
      required: true,
      autoComplete: 'organization',
      section: 'organization',
    },
    {
      name: 'jobTitle',
      type: 'text',
      label: 'Job title',
      autoComplete: 'organization-title',
      section: 'organization',
    },
    {
      name: 'productInterest',
      type: 'select',
      label: 'Product interest',
      required: true,
      placeholder: 'Select a product area',
      section: 'interest',
      options: [
        { value: 'aovias-command', label: 'AOVIAS Command' },
        { value: 'platform', label: 'Platform capabilities' },
        { value: 'integration', label: 'Integrations' },
        { value: 'other', label: 'Other' },
      ],
    },
    {
      name: 'preferredSchedule',
      type: 'select',
      label: 'Preferred schedule',
      required: true,
      placeholder: 'Select a timing window',
      description: 'We will propose concrete times within this window.',
      section: 'interest',
      options: [
        { value: 'within-2-weeks', label: 'Within 2 weeks' },
        { value: 'this-month', label: 'This month' },
        { value: 'next-quarter', label: 'Next quarter' },
        { value: 'flexible', label: 'Flexible' },
      ],
    },
    {
      name: 'preferredContact',
      type: 'radio',
      label: 'Preferred contact method',
      required: true,
      section: 'interest',
      options: [
        { value: 'email', label: 'Email' },
        { value: 'phone', label: 'Phone' },
        { value: 'either', label: 'Either' },
      ],
    },
    {
      name: 'notes',
      type: 'textarea',
      label: 'Additional notes',
      section: 'interest',
      colSpan: 2,
    },
  ],
};

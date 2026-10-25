import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import {
  consentField,
  emailField,
  lengthString,
  optionalLengthString,
  optionalPhoneField,
} from '@/features/forms/validators';

export const contactFormSchema = z.object({
  name: lengthString(2, 100),
  email: emailField(),
  organization: optionalLengthString(120),
  phone: optionalPhoneField(),
  subject: lengthString(2, 160),
  message: lengthString(10, 2000),
  consent: consentField(),
  /** Honeypot — must stay empty. */
  company_website: z.string().max(200).optional().default(''),
});

export type ContactFormValues = z.infer<typeof contactFormSchema>;

export const contactFormConfig: FormConfig<ContactFormValues> = {
  id: 'contact-form',
  ariaLabel: 'Contact form',
  schema: contactFormSchema,
  columns: 2,
  submitLabel: 'Send message',
  submittingLabel: 'Sending…',
  success: {
    title: 'Enquiry received',
    message:
      'Thank you for contacting AOVIAS. Our team has received your enquiry and will respond shortly.',
    followUp: 'We typically respond within one business day.',
    showIllustration: false,
  },
  sections: [
    {
      id: 'details',
      description: 'Your contact details',
    },
    {
      id: 'message',
      description: 'Your message',
    },
  ],
  defaultValues: {
    name: '',
    email: '',
    organization: '',
    phone: '',
    subject: '',
    message: '',
    consent: false,
    company_website: '',
  },
  fields: [
    {
      name: 'name',
      type: 'text',
      label: 'Full name',
      required: true,
      autoComplete: 'name',
      section: 'details',
    },
    {
      name: 'email',
      type: 'email',
      label: 'Work email',
      required: true,
      autoComplete: 'email',
      section: 'details',
    },
    {
      name: 'organization',
      type: 'text',
      label: 'Organization',
      autoComplete: 'organization',
      section: 'details',
    },
    {
      name: 'phone',
      type: 'tel',
      label: 'Phone',
      autoComplete: 'tel',
      section: 'details',
    },
    {
      name: 'subject',
      type: 'text',
      label: 'Subject',
      required: true,
      section: 'message',
      colSpan: 2,
    },
    {
      name: 'message',
      type: 'textarea',
      label: 'Message',
      required: true,
      section: 'message',
      colSpan: 2,
    },
    {
      name: 'consent',
      type: 'checkbox',
      label: 'I agree to be contacted about this inquiry.',
      required: true,
      section: 'message',
      colSpan: 2,
    },
    {
      name: 'company_website',
      type: 'honeypot',
      label: 'Company website',
      section: 'message',
      colSpan: 2,
    },
  ],
};

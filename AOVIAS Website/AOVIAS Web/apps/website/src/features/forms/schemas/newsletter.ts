import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import {
  consentField,
  emailField,
} from '@/features/forms/validators';

export const newsletterFormSchema = z.object({
  email: emailField(),
  productUpdates: z.boolean(),
  securityAdvisories: z.boolean(),
  events: z.boolean(),
  consent: consentField(),
});

export type NewsletterFormValues = z.infer<typeof newsletterFormSchema>;

export const newsletterFormConfig: FormConfig<NewsletterFormValues> = {
  id: 'newsletter-form',
  ariaLabel: 'Newsletter subscription form',
  schema: newsletterFormSchema,
  columns: 1,
  submitLabel: 'Subscribe',
  submittingLabel: 'Subscribing…',
  success: {
    title: 'Subscribed',
    message: 'You are on the list for selected AOVIAS updates.',
    followUp: 'You can update preferences any time from future emails.',
    showIllustration: true,
  },
  sections: [
    { id: 'email', title: 'Email' },
    {
      id: 'preferences',
      title: 'Preferences',
      description: 'Choose the topics you want to receive.',
    },
    { id: 'consent', title: 'Consent' },
  ],
  defaultValues: {
    email: '',
    productUpdates: true,
    securityAdvisories: true,
    events: false,
    consent: false,
  },
  fields: [
    {
      name: 'email',
      type: 'email',
      label: 'Email address',
      required: true,
      autoComplete: 'email',
      section: 'email',
    },
    {
      name: 'productUpdates',
      type: 'switch',
      label: 'Product updates',
      description: 'Release notes and capability announcements.',
      section: 'preferences',
    },
    {
      name: 'securityAdvisories',
      type: 'switch',
      label: 'Security advisories',
      description: 'Material security communications.',
      section: 'preferences',
    },
    {
      name: 'events',
      type: 'checkbox',
      label: 'Events and webinars',
      section: 'preferences',
    },
    {
      name: 'consent',
      type: 'checkbox',
      label: 'I consent to receive email communications from AOVIAS.',
      required: true,
      section: 'consent',
    },
  ],
};

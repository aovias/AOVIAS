import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import {
  emailField,
  lengthString,
  optionalLengthString,
  selectField,
} from '@/features/forms/validators';

const purchaseTypeValues = [
  'individual',
  'business',
  'enterprise-request',
] as const;

const countryValues = [
  'IN',
  'US',
  'GB',
  'AE',
  'SG',
  'AU',
  'CA',
  'OTHER',
] as const;

const currencyValues = ['USD'] as const;

export const billingFormSchema = z.object({
  purchaseType: selectField(purchaseTypeValues),
  contactName: lengthString(2, 100),
  email: emailField(),
  organizationName: lengthString(2, 120),
  taxIdentifier: optionalLengthString(40),
  country: selectField(countryValues),
  currencyCode: selectField(currencyValues),
  addressLine1: lengthString(2, 160),
  addressLine2: optionalLengthString(160),
  city: lengthString(2, 80),
  region: optionalLengthString(80),
  postalCode: lengthString(2, 24),
});

export type BillingFormSchemaValues = z.infer<typeof billingFormSchema>;

export const billingFormConfig: FormConfig<BillingFormSchemaValues> = {
  id: 'checkout-billing-form',
  ariaLabel: 'Billing information form',
  schema: billingFormSchema,
  columns: 2,
  submitLabel: 'Continue to confirmation',
  submittingLabel: 'Saving…',
  success: {
    title: 'Billing details saved',
    message: 'Review your order on the next step.',
  },
  sections: [
    { id: 'purchase', title: 'Purchase type' },
    { id: 'contact', title: 'Billing contact' },
    { id: 'organization', title: 'Organization' },
    { id: 'address', title: 'Billing address' },
  ],
  defaultValues: {
    purchaseType: 'business',
    contactName: '',
    email: '',
    organizationName: '',
    taxIdentifier: '',
    country: 'IN',
    currencyCode: 'USD',
    addressLine1: '',
    addressLine2: '',
    city: '',
    region: '',
    postalCode: '',
  },
  fields: [
    {
      name: 'purchaseType',
      type: 'select',
      label: 'Purchase type',
      required: true,
      section: 'purchase',
      colSpan: 2,
      placeholder: 'Select purchase type',
      options: [
        { value: 'individual', label: 'Individual' },
        { value: 'business', label: 'Business' },
        { value: 'enterprise-request', label: 'Enterprise request' },
      ],
    },
    {
      name: 'contactName',
      type: 'text',
      label: 'Billing contact',
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
      name: 'organizationName',
      type: 'text',
      label: 'Organization name',
      required: true,
      autoComplete: 'organization',
      section: 'organization',
    },
    {
      name: 'taxIdentifier',
      type: 'text',
      label: 'Tax identifier',
      description: 'Optional — for future tax invoicing.',
      section: 'organization',
    },
    {
      name: 'country',
      type: 'select',
      label: 'Country',
      required: true,
      section: 'organization',
      placeholder: 'Select country',
      options: [
        { value: 'IN', label: 'India' },
        { value: 'US', label: 'United States' },
        { value: 'GB', label: 'United Kingdom' },
        { value: 'AE', label: 'United Arab Emirates' },
        { value: 'SG', label: 'Singapore' },
        { value: 'AU', label: 'Australia' },
        { value: 'CA', label: 'Canada' },
        { value: 'OTHER', label: 'Other' },
      ],
    },
    {
      name: 'currencyCode',
      type: 'select',
      label: 'Currency',
      required: true,
      section: 'organization',
      options: [{ value: 'USD', label: 'USD' }],
    },
    {
      name: 'addressLine1',
      type: 'text',
      label: 'Address line 1',
      required: true,
      autoComplete: 'address-line1',
      section: 'address',
      colSpan: 2,
    },
    {
      name: 'addressLine2',
      type: 'text',
      label: 'Address line 2',
      autoComplete: 'address-line2',
      section: 'address',
      colSpan: 2,
    },
    {
      name: 'city',
      type: 'text',
      label: 'City',
      required: true,
      autoComplete: 'address-level2',
      section: 'address',
    },
    {
      name: 'region',
      type: 'text',
      label: 'State / region',
      autoComplete: 'address-level1',
      section: 'address',
    },
    {
      name: 'postalCode',
      type: 'text',
      label: 'Postal code',
      required: true,
      autoComplete: 'postal-code',
      section: 'address',
      colSpan: 2,
    },
  ],
};

import { z } from 'zod';

import type { FormConfig } from '@/features/forms/engine/types';
import { validationMessages } from '@/features/forms/validators/messages';
import { consentField } from '@/features/forms/validators';

export const checkoutConfirmFormSchema = z.object({
  acceptedTerms: consentField(
    'Confirm you accept the purchase terms to continue.',
  ),
  acceptedPrivacy: consentField(validationMessages.consent),
});

export type CheckoutConfirmFormValues = z.infer<
  typeof checkoutConfirmFormSchema
>;

export const checkoutConfirmFormConfig: FormConfig<CheckoutConfirmFormValues> =
  {
    id: 'checkout-confirm-form',
    ariaLabel: 'Order confirmation form',
    schema: checkoutConfirmFormSchema,
    columns: 1,
    submitLabel: 'Submit order request',
    submittingLabel: 'Submitting…',
    success: {
      title: 'Order recorded',
      message:
        'Your order request was recorded. Payment collection is not enabled in this environment.',
      followUp: 'Continue to the success page for next steps.',
    },
    defaultValues: {
      acceptedTerms: false,
      acceptedPrivacy: false,
    },
    fields: [
      {
        name: 'acceptedTerms',
        type: 'checkbox',
        label:
          'I confirm this purchase summary is accurate and accept the terms of sale.',
        required: true,
      },
      {
        name: 'acceptedPrivacy',
        type: 'checkbox',
        label:
          'I acknowledge the privacy notice for processing billing information.',
        required: true,
      },
    ],
  };

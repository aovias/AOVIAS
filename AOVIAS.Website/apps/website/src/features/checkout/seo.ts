export const checkoutSeo = {
  checkout: {
    title: 'Checkout',
    description:
      'Review your AOVIAS plan selection and continue to billing. Payment processing is not enabled in this experience.',
    path: '/checkout',
  },
  billing: {
    title: 'Billing information',
    description: 'Provide billing contact and address details for your AOVIAS purchase.',
    path: '/checkout/billing',
  },
  confirm: {
    title: 'Confirm order',
    description: 'Review your AOVIAS purchase summary and confirm terms before submitting.',
    path: '/checkout/confirm',
  },
  success: {
    title: 'Order received',
    description: 'Your AOVIAS checkout request was recorded. Next steps for access and support.',
    path: '/checkout/success',
  },
} as const;

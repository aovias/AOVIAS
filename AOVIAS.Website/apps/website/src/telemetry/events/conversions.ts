import type { EventName } from '../registry/event-names';

/**
 * Enterprise conversion contracts — names only; no live tracking in R1.7.
 * Map product funnels to `conversion.*` (and domain mirrors) for Release 2.
 */
export type ConversionContract = {
  id: string;
  label: string;
  description: string;
  /** Primary event name to emit when the conversion occurs. */
  eventName: EventName;
  /** Optional domain mirror events emitted alongside the conversion. */
  mirrorEvents?: readonly EventName[];
  stage: 'r1_defined' | 'r2_planned';
  funnel:
    | 'demand'
    | 'evaluation'
    | 'engagement'
    | 'talent'
    | 'commerce'
    | 'identity';
};

export const conversionContracts = [
  {
    id: 'demo-request',
    label: 'Demo Request',
    description: 'Institution requested a product demonstration.',
    eventName: 'conversion.demo_request',
    mirrorEvents: ['contact.demo_requested'],
    stage: 'r1_defined',
    funnel: 'demand',
  },
  {
    id: 'enterprise-inquiry',
    label: 'Enterprise Inquiry',
    description: 'Enterprise / procurement inquiry submitted.',
    eventName: 'conversion.enterprise_inquiry',
    mirrorEvents: ['contact.enterprise_inquiry'],
    stage: 'r1_defined',
    funnel: 'demand',
  },
  {
    id: 'partner-inquiry',
    label: 'Partner Inquiry',
    description: 'Partnership conversation requested.',
    eventName: 'conversion.partner_inquiry',
    mirrorEvents: ['contact.partner_inquiry'],
    stage: 'r1_defined',
    funnel: 'demand',
  },
  {
    id: 'pricing-interest',
    label: 'Pricing Interest',
    description: 'Meaningful engagement with published pricing.',
    eventName: 'conversion.pricing_interest',
    mirrorEvents: ['pricing.viewed', 'pricing.plan_selected'],
    stage: 'r1_defined',
    funnel: 'evaluation',
  },
  {
    id: 'product-exploration',
    label: 'Product Exploration',
    description: 'Product detail exploration beyond a bounce.',
    eventName: 'conversion.product_exploration',
    mirrorEvents: ['product.viewed'],
    stage: 'r1_defined',
    funnel: 'evaluation',
  },
  {
    id: 'documentation-visit',
    label: 'Documentation Visit',
    description: 'Documentation consumed during evaluation.',
    eventName: 'conversion.documentation_visit',
    mirrorEvents: ['resource.docs_visited'],
    stage: 'r1_defined',
    funnel: 'evaluation',
  },
  {
    id: 'career-interest',
    label: 'Career Interest',
    description: 'Careers page or role engagement.',
    eventName: 'conversion.career_interest',
    mirrorEvents: ['career.viewed', 'career.application_started'],
    stage: 'r1_defined',
    funnel: 'talent',
  },
  {
    id: 'newsletter-signup',
    label: 'Newsletter Signup',
    description: 'Marketing newsletter subscription (requires marketing consent).',
    eventName: 'conversion.newsletter_signup',
    stage: 'r1_defined',
    funnel: 'engagement',
  },
  {
    id: 'contact-submission',
    label: 'Contact Submission',
    description: 'General contact form submission.',
    eventName: 'conversion.contact_submission',
    mirrorEvents: ['contact.submitted'],
    stage: 'r1_defined',
    funnel: 'demand',
  },
  {
    id: 'checkout-started',
    label: 'Checkout Started',
    description: 'Future commerce: checkout started.',
    eventName: 'commerce.checkout_started',
    stage: 'r2_planned',
    funnel: 'commerce',
  },
  {
    id: 'checkout-completed',
    label: 'Checkout Completed',
    description: 'Future commerce: checkout completed.',
    eventName: 'commerce.checkout_completed',
    stage: 'r2_planned',
    funnel: 'commerce',
  },
  {
    id: 'auth-started',
    label: 'Authentication Started',
    description: 'Future identity: auth flow started.',
    eventName: 'identity.auth_started',
    stage: 'r2_planned',
    funnel: 'identity',
  },
  {
    id: 'auth-completed',
    label: 'Authentication Completed',
    description: 'Future identity: auth flow completed.',
    eventName: 'identity.auth_completed',
    stage: 'r2_planned',
    funnel: 'identity',
  },
  {
    id: 'trial-started',
    label: 'Trial Started',
    description: 'Future commerce: trial started.',
    eventName: 'commerce.trial_started',
    stage: 'r2_planned',
    funnel: 'commerce',
  },
] as const satisfies readonly ConversionContract[];

export type ConversionContractId = (typeof conversionContracts)[number]['id'];

export function getConversionContract(
  id: ConversionContractId,
): ConversionContract | undefined {
  return conversionContracts.find((contract) => contract.id === id);
}

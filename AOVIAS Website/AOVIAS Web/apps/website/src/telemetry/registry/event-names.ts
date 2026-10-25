/**
 * Canonical event name union — single source for typed track() calls.
 */

export const EVENT_NAMES = [
  // page
  'page.viewed',
  // navigation
  'navigation.opened',
  'navigation.item_selected',
  'navigation.closed',
  // product
  'product.viewed',
  'product.cta_clicked',
  // technology
  'technology.page_viewed',
  'technology.ai_page_viewed',
  'technology.platform_page_viewed',
  // pricing
  'pricing.viewed',
  'pricing.plan_selected',
  // contact
  'contact.form_started',
  'contact.submitted',
  'contact.demo_requested',
  'contact.enterprise_inquiry',
  'contact.partner_inquiry',
  // career
  'career.viewed',
  'career.application_started',
  // search
  'search.executed',
  // resource
  'resource.viewed',
  'resource.downloaded',
  'resource.docs_visited',
  // conversion (explicit conversion.* aliases where useful)
  'conversion.demo_request',
  'conversion.enterprise_inquiry',
  'conversion.partner_inquiry',
  'conversion.pricing_interest',
  'conversion.product_exploration',
  'conversion.documentation_visit',
  'conversion.career_interest',
  'conversion.newsletter_signup',
  'conversion.contact_submission',
  // identity (future)
  'identity.auth_started',
  'identity.auth_completed',
  // commerce (future)
  'commerce.checkout_started',
  'commerce.checkout_completed',
  'commerce.trial_started',
  // interaction
  'page.cta_clicked',
  // error
  'error.captured',
] as const;

export type EventName = (typeof EVENT_NAMES)[number];

export function isEventName(value: string): value is EventName {
  return (EVENT_NAMES as readonly string[]).includes(value);
}

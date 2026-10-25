/**
 * Engagement model — configuration-driven contact pathways.
 * Add future types here without changing page infrastructure.
 */

export type EngagementType =
  | 'sales'
  | 'demo-request'
  | 'enterprise-inquiry'
  | 'healthcare-solutions'
  | 'government-engagement'
  | 'partner-inquiry'
  | 'general-contact';

export type EngagementOption = {
  id: EngagementType;
  title: string;
  description: string;
  /** Expected response framing for visitors. */
  responseExpectation: string;
  /** In-page anchor to the matching form region. */
  href: string;
  linkLabel: string;
};

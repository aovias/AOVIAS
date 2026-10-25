import type { EngagementOption } from '@/features/contact/domain/types';

/**
 * Engagement options — enterprise inquiry paths (Phase V9).
 */
export const engagementOptions: readonly EngagementOption[] = [
  {
    id: 'sales',
    title: 'Enterprise Sales',
    description:
      'Discuss commercial fit, licensing models, and how AOVIAS supports institutional priorities.',
    responseExpectation: 'Typical response within two business days.',
    href: '#form-contact',
    linkLabel: 'Contact sales',
  },
  {
    id: 'demo-request',
    title: 'Platform Evaluation',
    description:
      'Request a focused demonstration of Command or Studio with your evaluation stakeholders.',
    responseExpectation: 'We propose times within your preferred schedule window.',
    href: '#form-demo',
    linkLabel: 'Request a demo',
  },
  {
    id: 'enterprise-inquiry',
    title: 'Enterprise Engagement',
    description:
      'Explore procurement constraints, security review, and multi-team or multi-site rollout.',
    responseExpectation: 'An enterprise specialist follows up with next steps.',
    href: '#form-enterprise',
    linkLabel: 'Contact enterprise',
  },
  {
    id: 'healthcare-solutions',
    title: 'Healthcare Solutions',
    description:
      'Engage on clinical operations, Command adoption, and healthcare institutional requirements.',
    responseExpectation: 'A healthcare-aware specialist reviews your context.',
    href: '#form-enterprise',
    linkLabel: 'Discuss healthcare',
  },
  {
    id: 'government-engagement',
    title: 'Government Engagement',
    description:
      'Discuss public-sector constraints, assurance expectations, and governed deployment models.',
    responseExpectation: 'We route government inquiries carefully before responding.',
    href: '#form-enterprise',
    linkLabel: 'Discuss government',
  },
  {
    id: 'partner-inquiry',
    title: 'Partnerships',
    description:
      'Propose technology, reseller, systems-integrator, or advisory partnerships.',
    responseExpectation: 'Partnerships reviews inquiries thoughtfully before responding.',
    href: '#form-partner',
    linkLabel: 'Explore partnership',
  },
  {
    id: 'general-contact',
    title: 'Technical Questions',
    description:
      'Reach AOVIAS for architecture questions, media, or inquiries that do not fit other paths.',
    responseExpectation: 'We typically respond within two business days.',
    href: '#form-contact',
    linkLabel: 'Send a message',
  },
] as const;

export function getEngagementOption(
  id: EngagementOption['id'],
): EngagementOption | undefined {
  return engagementOptions.find((option) => option.id === id);
}

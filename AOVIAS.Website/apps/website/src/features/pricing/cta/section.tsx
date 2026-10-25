import { Section } from '@/components/layout';
import { EnterpriseCta } from '@/components/pricing';
import type { EnterpriseOffering } from '@/components/pricing';

const primaryOffering: EnterpriseOffering = {
  id: 'pricing-primary-cta',
  title: 'Ready for the next step?',
  description:
    'Request a focused demo, Contact enterprise for tailored licensing, or Talk with AOVIAS with a general inquiry. We respond with a practical next step.',
  primaryCta: {
    label: 'Request a demo',
    href: '/company/contact#form-demo',
  },
  secondaryCta: {
    label: 'Contact enterprise',
    href: '/company/contact#form-enterprise',
  },
  tertiaryCta: {
    label: 'Talk with AOVIAS',
    href: '/company/contact#form-contact',
  },
};

/**
 * Primary pricing CTA — demo / enterprise / contact.
 */
export function PricingCtaSection() {
  return (
    <Section spacing="md" id="pricing-cta">
      <EnterpriseCta offering={primaryOffering} background="muted" />
    </Section>
  );
}

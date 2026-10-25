import { CtaSection } from '@/components/marketing';
import { ctaContent, ctaMeta } from '@/features/careers/cta/content';

/**
 * Primary Careers CTA — roles, application, recruiting contact.
 */
export function CareersCtaSection() {
  return (
    <div id={ctaMeta.sectionId}>
      <CtaSection
        title={ctaContent.title}
        description={ctaContent.description}
        primaryCta={ctaContent.primaryCta}
        secondaryCta={ctaContent.secondaryCta}
        background="dark"
      />
    </div>
  );
}

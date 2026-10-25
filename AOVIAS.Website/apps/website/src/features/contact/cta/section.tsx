import { CtaSection } from '@/components/marketing';
import { ctaContent, ctaMeta } from '@/features/contact/cta/content';

/**
 * Primary Contact CTA — guidance when the path is unclear.
 */
export function ContactCtaSection() {
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

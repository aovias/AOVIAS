import { Hero } from '@/components/marketing';
import { heroContent, heroMeta } from '@/features/contact/hero/content';

/**
 * Contact hero — compact invitation; engagement continues in the forms band.
 * Illustration removed to reduce unused whitespace (R1.CONTACT.3).
 */
export function ContactHeroSection() {
  return (
    <div id={heroMeta.sectionId} className="scroll-mt-24">
      <Hero
        eyebrow={heroContent.eyebrow}
        title={heroContent.title}
        description={heroContent.description}
        primaryCta={{
          label: heroContent.primaryCta.label,
          href: '#form-demo',
        }}
        secondaryCta={{
          label: heroContent.secondaryCta.label,
          href: '#form-enterprise',
        }}
        atmosphere="corporate-gradient"
        align="start"
      />
    </div>
  );
}

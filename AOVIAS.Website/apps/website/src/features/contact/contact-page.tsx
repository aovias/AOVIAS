import { ContactCtaSection } from '@/features/contact/cta/section';
import { DemoExperienceSection } from '@/features/contact/demo/demo-experience-section';
import { ContactFaqSection } from '@/features/contact/faq/section';
import { ContactFormsBandLazy } from '@/features/contact/forms/forms-band-lazy';
import { ContactHeroSection } from '@/features/contact/hero/section';
import { OfficeInformationSection } from '@/features/contact/office/section';

/**
 * Contact & Enterprise Engagement — procurement-ready inquiry hub.
 * Intent selection + form live in ContactFormsBand (R1.CONTACT.3).
 */
export function ContactPage() {
  return (
    <>
      <ContactHeroSection />
      <ContactFormsBandLazy />
      <DemoExperienceSection />
      <OfficeInformationSection />
      <ContactFaqSection />
      <ContactCtaSection />
    </>
  );
}

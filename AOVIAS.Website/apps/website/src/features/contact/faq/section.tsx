import { SectionHeader } from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { Faq } from '@/components/trust';
import {
  contactFaqItems,
  faqContent,
  faqMeta,
} from '@/features/contact/faq/content';

/**
 * Contact FAQ — compact engagement Q&A via Trust Faq.
 */
export function ContactFaqSection() {
  return (
    <section
      id={faqMeta.sectionId}
      className="w-full scroll-mt-24 bg-muted/30 py-16 lg:py-20"
      aria-label={faqMeta.title}
    >
      <Container width="wide">
        <Stack gap={8}>
          <SectionHeader
            eyebrow={faqContent.eyebrow}
            heading={faqContent.heading}
            description={faqContent.description}
          />
          <Faq items={contactFaqItems} groupByCategory />
        </Stack>
      </Container>
    </section>
  );
}

import { SectionHeader } from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { Faq } from '@/components/trust';
import {
  companyFaqItems,
  faqContent,
  faqMeta,
} from '@/features/company/faq/content';

/**
 * Company FAQ — compact institutional Q&A via Trust Faq.
 */
export function CompanyFaqSection() {
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
          <Faq items={companyFaqItems} groupByCategory />
        </Stack>
      </Container>
    </section>
  );
}

import Link from 'next/link';

import { MarketingReveal } from '@/components/marketing/reveal';
import { SectionHeader } from '@/components/marketing';
import { Container, Paragraph, Stack, Text } from '@/components/primitives';
import {
  constitutionContent,
  constitutionMeta,
} from '@/features/company/constitution/content';

const principleLabels = [
  'Trust',
  'Discipline',
  'Stewardship',
  'Long-term Commitment',
] as const;

/**
 * Constitution Summary — editorial public summary (not internal governance docs).
 */
export function ConstitutionSection() {
  return (
    <section
      id={constitutionMeta.sectionId}
      className="w-full py-16 lg:py-20"
      aria-label={constitutionMeta.title}
    >
      <Container width="narrow">
        <MarketingReveal>
          <Stack gap={8} className="max-w-3xl">
            <SectionHeader
              eyebrow={constitutionContent.eyebrow}
              heading={constitutionContent.heading}
              description={constitutionContent.description}
              width="wide"
            />

            <div
              aria-hidden
              className="border-border/70 h-px w-full border-t"
            />

            <ul className="m-0 flex list-none flex-wrap items-center gap-x-3 gap-y-2 p-0 sm:gap-x-0">
              {principleLabels.map((label, index) => (
                <li
                  key={label}
                  className="text-foreground flex items-center gap-3 text-small font-medium tracking-wide"
                >
                  {index > 0 ? (
                    <span
                      aria-hidden
                      className="text-muted-foreground/50 hidden sm:inline"
                    >
                      ·
                    </span>
                  ) : null}
                  <span>{label}</span>
                </li>
              ))}
            </ul>

            <div
              aria-hidden
              className="border-border/70 h-px w-full border-t"
            />

            <div className="sr-only">
              {constitutionContent.principles.map((item) => (
                <div key={item.id}>
                  <Text as="p" weight="semibold">
                    {item.principle}
                  </Text>
                  <Paragraph size="small">{item.description}</Paragraph>
                </div>
              ))}
            </div>

            <Link
              href="/company/contact"
              className="text-primary focus-visible:ring-focus-ring inline-flex w-fit items-center gap-1.5 text-small font-medium underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
            >
              Read Constitution
              <span aria-hidden="true">→</span>
            </Link>
          </Stack>
        </MarketingReveal>
      </Container>
    </section>
  );
}

import { MarketingReveal } from '@/components/marketing/reveal';
import { SectionHeader, SectionSurface } from '@/components/marketing';
import { Paragraph, Stack, Text } from '@/components/primitives';
import {
  philosophyContent,
  philosophyMeta,
} from '@/features/company/philosophy/content';

/**
 * Philosophy — numbered institutional principles (editorial list).
 */
export function PhilosophySection() {
  return (
    <SectionSurface
      id={philosophyMeta.sectionId}
      tone="elevated"
      bordered
      spacing="lg"
      aria-label={philosophyMeta.title}
    >
      <Stack gap={10}>
        <MarketingReveal>
          <SectionHeader
            eyebrow={philosophyContent.eyebrow}
            heading={philosophyContent.heading}
            description={philosophyContent.description}
          />
        </MarketingReveal>

        <ol className="m-0 flex list-none flex-col gap-0 p-0">
          {philosophyContent.principles.map((item, index) => (
            <li
              key={item.id}
              className="border-border/60 grid gap-4 border-b py-8 last:border-b-0 md:grid-cols-[5.5rem_minmax(0,1fr)] md:gap-10 md:py-10"
            >
              <span className="text-domain-accent/80 text-3xl font-semibold tracking-[0.08em] tabular-nums md:text-4xl">
                {String(index + 1).padStart(2, '0')}
              </span>
              <div className="min-w-0 max-w-2xl">
                <Text
                  as="p"
                  weight="semibold"
                  size="body-lg"
                  className="text-foreground tracking-tight"
                >
                  {item.principle}
                </Text>
                <Paragraph size="body" tone="muted" className="mt-2">
                  {item.description}
                </Paragraph>
              </div>
            </li>
          ))}
        </ol>
      </Stack>
    </SectionSurface>
  );
}

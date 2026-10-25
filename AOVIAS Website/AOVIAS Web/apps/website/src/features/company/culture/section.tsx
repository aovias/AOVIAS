import { MarketingReveal } from '@/components/marketing/reveal';
import { SectionHeader, SectionSurface } from '@/components/marketing';
import { Paragraph, Stack, Text } from '@/components/primitives';
import { cultureContent, cultureMeta } from '@/features/company/culture/content';
import { cn } from '@/lib/utils';

const masonrySpan = [
  'md:row-span-2 md:min-h-[14rem]',
  'md:min-h-[9rem]',
  'md:min-h-[11rem]',
  'md:row-span-2 md:min-h-[13rem]',
  'md:min-h-[10rem]',
] as const;

/**
 * Culture — editorial masonry / asymmetric grid.
 */
export function CultureSection() {
  return (
    <SectionSurface
      id={cultureMeta.sectionId}
      tone="feature"
      atmosphere="corporate-gradient"
      spacing="lg"
      aria-label={cultureMeta.title}
    >
      <Stack gap={10}>
        <MarketingReveal>
          <SectionHeader
            eyebrow={cultureContent.eyebrow}
            heading={cultureContent.heading}
            description={cultureContent.description}
          />
        </MarketingReveal>

        <ul className="m-0 grid list-none gap-4 p-0 md:auto-rows-[minmax(0,auto)] md:grid-cols-3 md:gap-5">
          {cultureContent.principles.map((item, index) => (
            <li
              key={item.id}
              className={cn(
                'border-border/50 bg-card/35 flex flex-col justify-end rounded-xl border p-5 ring-1 ring-inset ring-white/[0.03]',
                masonrySpan[index] ?? 'md:min-h-[10rem]',
              )}
            >
              <Text
                as="p"
                weight="semibold"
                size="body"
                className="text-foreground tracking-tight"
              >
                {item.title}
              </Text>
              <Paragraph size="small" tone="muted" className="mt-2">
                {item.description}
              </Paragraph>
            </li>
          ))}
        </ul>
      </Stack>
    </SectionSurface>
  );
}

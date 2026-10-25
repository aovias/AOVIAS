import { MarketingReveal } from '@/components/marketing/reveal';
import { SectionHeader, SectionSurface } from '@/components/marketing';
import { Paragraph, Stack, Text } from '@/components/primitives';
import { visionContent, visionMeta } from '@/features/company/vision/content';
import { cn } from '@/lib/utils';

/**
 * Vision — centered heading + three pillars on a restrained institutional field.
 */
export function VisionSection() {
  return (
    <SectionSurface
      id={visionMeta.sectionId}
      tone="feature"
      spacing="lg"
      aria-label={visionMeta.title}
      className="relative overflow-hidden"
    >
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-[0.07]"
        style={{
          backgroundImage:
            'linear-gradient(to right, currentColor 1px, transparent 1px), linear-gradient(to bottom, currentColor 1px, transparent 1px)',
          backgroundSize: '48px 48px',
        }}
      />
      <div
        aria-hidden
        className="bg-domain-accent/10 pointer-events-none absolute top-1/2 left-1/2 h-[28rem] w-[28rem] -translate-x-1/2 -translate-y-1/2 rounded-full blur-3xl"
      />

      <Stack gap={10} className="relative">
        <MarketingReveal>
          <Stack gap={4} className="mx-auto max-w-3xl">
            <SectionHeader
              align="center"
              eyebrow={visionContent.eyebrow}
              heading={visionContent.heading}
              description={visionContent.description}
            />
            <Paragraph
              size="body-lg"
              tone="muted"
              className="mx-auto max-w-2xl text-center"
            >
              {visionContent.statement}
            </Paragraph>
          </Stack>
        </MarketingReveal>

        <ul className="mx-auto grid max-w-5xl list-none gap-6 md:grid-cols-3 md:gap-8">
          {visionContent.pillars.map((item, index) => (
            <li
              key={item.id}
              className={cn(
                'border-border/50 bg-card/40 relative rounded-xl border px-5 py-6 text-center',
                'ring-1 ring-inset ring-white/[0.03]',
              )}
            >
              <Text
                as="p"
                size="caption"
                className="text-domain-accent mb-3 font-semibold tracking-[0.14em] uppercase"
              >
                {String(index + 1).padStart(2, '0')}
              </Text>
              <Text
                as="p"
                weight="semibold"
                size="body"
                className="text-foreground tracking-tight"
              >
                {item.principle}
              </Text>
              <Paragraph size="small" tone="muted" className="mt-2.5">
                {item.description}
              </Paragraph>
            </li>
          ))}
        </ul>
      </Stack>
    </SectionSurface>
  );
}

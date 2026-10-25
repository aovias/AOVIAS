import { InstitutionalTrust } from '@/components/brand';
import { MarketingReveal } from '@/components/marketing/reveal';
import { SectionHeader, SectionSurface } from '@/components/marketing';
import { Paragraph, Stack, Text } from '@/components/primitives';
import { missionContent, missionMeta } from '@/features/company/mission/content';

/**
 * Mission — asymmetric text + institutional illustration (distinct from Vision).
 */
export function MissionSection() {
  return (
    <SectionSurface
      id={missionMeta.sectionId}
      tone="muted"
      bordered
      spacing="lg"
      aria-label={missionMeta.title}
    >
      <div className="grid items-center gap-10 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)] lg:gap-14">
        <MarketingReveal>
          <Stack gap={5} className="max-w-xl">
            <SectionHeader
              eyebrow={missionContent.eyebrow}
              heading={missionContent.heading}
              description={missionContent.description}
            />
            <Paragraph size="body-lg">{missionContent.statement}</Paragraph>
            <ul className="mt-2 flex list-none flex-col gap-4 border-t border-border/50 pt-6">
              {missionContent.outcomes.map((item) => (
                <li key={item.id} className="min-w-0">
                  <Text
                    as="p"
                    weight="semibold"
                    size="small"
                    className="text-foreground"
                  >
                    {item.heading}
                  </Text>
                  <Paragraph size="small" tone="muted" className="mt-1">
                    {item.description}
                  </Paragraph>
                </li>
              ))}
            </ul>
          </Stack>
        </MarketingReveal>
        <MarketingReveal>
          <InstitutionalTrust
            title="Institutional trust — mission visual"
            className="min-h-[16rem] xl:min-h-[20rem]"
          />
        </MarketingReveal>
      </div>
    </SectionSurface>
  );
}

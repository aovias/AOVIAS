import { SectionHeader, Timeline } from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import {
  hiringContent,
  hiringMeta,
} from '@/features/careers/hiring-process/content';

/**
 * Hiring process — reusable Timeline stages.
 */
export function HiringProcessSection() {
  return (
    <section
      id={hiringMeta.sectionId}
      className="bg-muted/20 w-full py-16 lg:py-20"
      aria-label={hiringMeta.title}
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow={hiringContent.eyebrow}
            heading={hiringContent.heading}
            description={hiringContent.description}
          />
          <Timeline events={hiringContent.stages} orientation="vertical" />
        </Stack>
      </Container>
    </section>
  );
}

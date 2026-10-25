import { PlatformEcosystem } from '@/components/brand';
import { MarketingReveal } from '@/components/marketing/reveal';
import { QuoteBlock, SectionHeader } from '@/components/marketing';
import { Container, Paragraph, Stack } from '@/components/primitives';
import { storyContent, storyMeta } from '@/features/company/story/content';

/**
 * Our Story — purpose-led narrative with ecosystem visualization.
 */
export function StorySection() {
  return (
    <section
      id={storyMeta.sectionId}
      className="bg-muted/30 w-full py-16 lg:py-20 xl:py-24"
      aria-label={storyMeta.title}
    >
      <Container width="wide">
        <MarketingReveal>
          <div className="grid items-start gap-10 lg:grid-cols-2 lg:gap-14 xl:gap-16">
            <Stack gap={6}>
              <SectionHeader
                eyebrow={storyContent.eyebrow}
                heading={storyContent.heading}
                description={storyContent.description}
              />
              {storyContent.paragraphs.map((paragraph) => (
                <Paragraph key={paragraph.slice(0, 32)} size="body-lg" tone="muted">
                  {paragraph}
                </Paragraph>
              ))}
              <QuoteBlock
                quote="Purpose before products — institutions first."
                author="AOVIAS"
                position="Company principle"
                avatarInitials="AV"
              />
            </Stack>
            <PlatformEcosystem
              title="AOVIAS ecosystem"
              caption="From platform foundation to societal impact"
            />
          </div>
        </MarketingReveal>
      </Container>
    </section>
  );
}

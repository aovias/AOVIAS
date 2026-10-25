import { PrincipleCard, FeatureGrid, SectionHeader } from '@/components/marketing';
import { Container, Stack } from '@/components/primitives';
import { TechnologyStoryFlow } from '@/features/technology/editorial/technology-story-flow';

/**
 * Demo experience — consulting engagement transparency (Phase V9).
 */
export function DemoExperienceSection() {
  return (
    <section
      id="demo-experience"
      className="border-border bg-muted/20 w-full scroll-mt-24 border-y py-16 lg:py-20"
      aria-label="Demo experience"
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow="Demo experience"
            heading="What happens after you request a demo"
            description="A demonstration begins a consulting engagement — discovery, technical discussion, and a focused walkthrough aligned to your evaluation."
          />
          <div className="grid items-start gap-10 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)] xl:gap-14">
            <FeatureGrid cols={2} stagger={false}>
              <PrincipleCard
                principle="Confirmation"
                description="We acknowledge your request and confirm the product and stakeholders in scope."
              />
              <PrincipleCard
                principle="Response timing"
                description="Typical scheduling outreach within two business days of a complete submission."
              />
              <PrincipleCard
                principle="Discovery"
                description="A short conversation to understand environment, constraints, and evaluation goals."
              />
              <PrincipleCard
                principle="Demonstration"
                description="A focused technical walkthrough — not a generic marketing presentation."
              />
            </FeatureGrid>
            <TechnologyStoryFlow
              title="Demo engagement path"
              steps={[
                'Submit request',
                'Confirm scope',
                'Discovery call',
                'Technical demo',
                'Follow-up',
                'Next steps',
              ]}
              caption="Consulting engagement"
            />
          </div>
        </Stack>
      </Container>
    </section>
  );
}

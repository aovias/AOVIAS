import { MarketingReveal } from '@/components/marketing/reveal';
import { SectionHeader } from '@/components/marketing';
import { AuthorBlock } from '@/components/resources';
import {
  Container,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import {
  leadershipContent,
  leadershipMeta,
} from '@/features/company/leadership/content';

/**
 * Leadership — two-column: founder stewardship + leadership principles.
 */
export function LeadershipSection() {
  return (
    <section
      id={leadershipMeta.sectionId}
      className="border-border bg-muted/20 w-full border-y py-16 lg:py-20"
      aria-label={leadershipMeta.title}
    >
      <Container width="wide">
        <div className="grid items-start gap-10 lg:grid-cols-2 lg:gap-14">
          <MarketingReveal>
            <Stack gap={6} className="max-w-xl">
              <SectionHeader
                eyebrow={leadershipContent.eyebrow}
                heading={leadershipContent.heading}
                description={leadershipContent.description}
              />
              <Paragraph tone="muted">{leadershipContent.philosophy}</Paragraph>
              {leadershipContent.profiles.map((author) => (
                <AuthorBlock
                  key={author.id}
                  author={author}
                  variant="detailed"
                />
              ))}
            </Stack>
          </MarketingReveal>

          <MarketingReveal>
            <Stack gap={5}>
              <Text as="p" size="small" weight="semibold" className="text-foreground">
                Leadership Principles
              </Text>
              <ul className="m-0 flex list-none flex-col gap-0 border-t border-border/60 p-0">
                {leadershipContent.principles.map((item) => (
                  <li
                    key={item.id}
                    className="border-border/60 border-b py-5 last:border-b-0"
                  >
                    <Text
                      as="p"
                      weight="semibold"
                      size="small"
                      className="text-foreground"
                    >
                      {item.title}
                    </Text>
                    <Paragraph size="small" tone="muted" className="mt-1.5">
                      {item.description}
                    </Paragraph>
                  </li>
                ))}
              </ul>
            </Stack>
          </MarketingReveal>
        </div>
      </Container>
    </section>
  );
}

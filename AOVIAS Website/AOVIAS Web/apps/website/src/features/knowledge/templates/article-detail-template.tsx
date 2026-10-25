import {
  formatPublishDate,
  formatReadingTime,
} from '@/components/resources/utils';
import { AuthorBlock, ResourceGrid, TableOfContents } from '@/components/resources';
import {
  Container,
  Heading,
  Link,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { ReadingProgress } from '@/components/resources';
import {
  getAdjacentKnowledge,
  getKnowledgeByKind,
  getRelatedKnowledge,
} from '@/features/knowledge/domain';
import type { KnowledgeDetailContent } from '@/features/knowledge/detail/types';

export type ArticleDetailTemplateProps = {
  content: KnowledgeDetailContent;
  /** Kind list used for previous/next navigation. */
  sectionKinds: ReadonlyArray<'article' | 'insight' | 'resource' | 'release-note'>;
};

/**
 * Editorial detail template — Blog, Insights, and library resources.
 */
export function ArticleDetailTemplate({
  content,
  sectionKinds,
}: ArticleDetailTemplateProps) {
  const { item, body, toc, relatedIds } = content;
  const related = getRelatedKnowledge(item, 3, relatedIds);
  const sectionItems = getKnowledgeByKind([...sectionKinds]);
  const { previous, next } = getAdjacentKnowledge(sectionItems, item.id);
  const meta = [
    formatPublishDate(item.publishedAt),
    formatReadingTime(item.readingTimeMinutes),
    item.category?.name,
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <article className="w-full py-10 lg:py-14" aria-labelledby="article-title">
      <ReadingProgress targetSelector="article" />
      <Container width="wide">
        <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_16rem]">
          <Stack gap={8}>
            <header className="max-w-3xl">
              <Text as="p" size="small" tone="muted">
                {item.kind === 'insight'
                  ? 'Insights'
                  : item.kind === 'article'
                    ? 'Blog'
                    : 'Resources'}
              </Text>
              <Heading id="article-title" level={1} size="display-l" className="mt-2">
                {item.title}
              </Heading>
              {item.description ? (
                <Paragraph size="body-lg" tone="muted" className="mt-4">
                  {item.description}
                </Paragraph>
              ) : null}
              {meta ? (
                <Text as="p" size="small" tone="muted" className="mt-4">
                  {meta}
                </Text>
              ) : null}
            </header>

            {item.authors?.[0] ? (
              <AuthorBlock author={item.authors[0]} variant="compact" />
            ) : null}

            <div className="max-w-3xl space-y-10 [&_p]:text-pretty [&_p]:leading-relaxed">
              {body.map((section, index) => (
                <section key={section.id} id={section.id} aria-labelledby={`${section.id}-heading`}>
                  {section.heading ? (
                    <Heading id={`${section.id}-heading`} level={2} size="h2">
                      {section.heading}
                    </Heading>
                  ) : null}
                  <Stack gap={4} {...(section.heading ? { className: 'mt-4' } : {})}>
                    {section.paragraphs.map((paragraph, pIndex) =>
                      index === 1 && pIndex === 0 ? (
                        <aside
                          key={paragraph.slice(0, 48)}
                          className="border-domain-accent/30 bg-domain-accent/5 border-s-2 px-4 py-3"
                        >
                          <Paragraph tone="default" className="text-foreground font-medium">
                            {paragraph}
                          </Paragraph>
                        </aside>
                      ) : (
                        <Paragraph key={paragraph.slice(0, 48)} tone="muted">
                          {paragraph}
                        </Paragraph>
                      ),
                    )}
                  </Stack>
                </section>
              ))}
            </div>

            <nav
              aria-label="Previous and next"
              className="border-border flex flex-col gap-4 border-t pt-8 sm:flex-row sm:justify-between"
            >
              {previous ? (
                <Link href={previous.href} tone="primary" className="text-small font-medium">
                  ← {previous.title}
                </Link>
              ) : (
                <span />
              )}
              {next ? (
                <Link href={next.href} tone="primary" className="text-small font-medium sm:text-right">
                  {next.title} →
                </Link>
              ) : null}
            </nav>

            {related.length > 0 ? (
              <Stack gap={4}>
                <Heading level={2} size="h3">
                  Related knowledge
                </Heading>
                <ResourceGrid items={related} cols={2} />
              </Stack>
            ) : null}
          </Stack>

          <aside className="hidden lg:block">
            <TableOfContents items={toc} sticky title="On this page" />
          </aside>
        </div>
      </Container>
    </article>
  );
}

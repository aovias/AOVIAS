import {
  formatPublishDate,
  formatReadingTime,
} from '@/components/resources/utils';
import { ResourceGrid, TableOfContents } from '@/components/resources';
import {
  Heading,
  Link,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import {
  getAdjacentKnowledge,
  getKnowledgeByKind,
  getRelatedKnowledge,
} from '@/features/knowledge/domain';
import type { KnowledgeDetailContent } from '@/features/knowledge/detail/types';

export type DocsDetailTemplateProps = {
  content: KnowledgeDetailContent;
};

/**
 * Documentation detail template — used under DocumentationLayout.
 */
export function DocsDetailTemplate({ content }: DocsDetailTemplateProps) {
  const { item, body, toc, relatedIds } = content;
  const docs = getKnowledgeByKind(['documentation']);
  const related = getRelatedKnowledge(item, 3, relatedIds);
  const { previous, next } = getAdjacentKnowledge(docs, item.id);
  const meta = [
    formatPublishDate(item.updatedAt ?? item.publishedAt),
    formatReadingTime(item.readingTimeMinutes),
  ]
    .filter(Boolean)
    .join(' · ');

  return (
    <div className="grid gap-10 lg:grid-cols-[14rem_minmax(0,1fr)_14rem]">
      <nav aria-label="Documentation" className="hidden lg:block">
        <Text as="p" size="small" weight="semibold" className="mb-3">
          Documentation
        </Text>
        <ul className="space-y-2">
          {docs.map((doc) => {
            const active = doc.id === item.id;
            return (
              <li key={doc.id}>
                <Link
                  href={doc.href}
                  className={
                    active
                      ? 'text-foreground text-small font-medium'
                      : 'text-muted-foreground text-small hover:text-foreground'
                  }
                  aria-current={active ? 'page' : undefined}
                >
                  {doc.title}
                </Link>
              </li>
            );
          })}
        </ul>
      </nav>

      <article aria-labelledby="doc-title">
        <Stack gap={8}>
          <header>
            <Text as="p" size="small" tone="muted">
              Documentation
            </Text>
            <Heading id="doc-title" level={1} size="h1" className="mt-2">
              {item.title}
            </Heading>
            {item.description ? (
              <Paragraph tone="muted" className="mt-3">
                {item.description}
              </Paragraph>
            ) : null}
            {meta ? (
              <Text as="p" size="caption" tone="muted" className="mt-3">
                {meta}
              </Text>
            ) : null}
          </header>

          <div className="space-y-8">
            {body.map((section) => (
              <section key={section.id} id={section.id}>
                {section.heading ? (
                  <Heading level={2} size="h2">
                    {section.heading}
                  </Heading>
                ) : null}
                <Stack gap={3} {...(section.heading ? { className: 'mt-3' } : {})}>
                  {section.paragraphs.map((paragraph) => (
                    <Paragraph key={paragraph.slice(0, 48)} tone="muted">
                      {paragraph}
                    </Paragraph>
                  ))}
                </Stack>
              </section>
            ))}
          </div>

          <nav
            aria-label="Previous and next documentation"
            className="border-border flex flex-col gap-3 border-t pt-6 sm:flex-row sm:justify-between"
          >
            {previous ? (
              <Link href={previous.href} tone="primary" className="text-small font-medium">
                ← {previous.title}
              </Link>
            ) : (
              <span />
            )}
            {next ? (
              <Link href={next.href} tone="primary" className="text-small font-medium">
                {next.title} →
              </Link>
            ) : null}
          </nav>

          {related.length > 0 ? (
            <Stack gap={4}>
              <Heading level={2} size="h3">
                Related
              </Heading>
              <ResourceGrid items={related} cols={2} />
            </Stack>
          ) : null}
        </Stack>
      </article>

      <aside className="hidden xl:block">
        <TableOfContents items={toc} sticky title="On this page" />
      </aside>
    </div>
  );
}

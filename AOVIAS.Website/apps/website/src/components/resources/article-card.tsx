import Link from 'next/link';

import type { ContentItem } from '@/components/content';
import { MarketingReveal } from '@/components/marketing/reveal';
import { CategoryBadge } from '@/components/resources/category-badge';
import { ContentImageView } from '@/components/resources/content-image';
import {
  formatPublishDate,
  formatReadingTime,
} from '@/components/resources/utils';
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * ArticleCard — content-agnostic card for any ContentItem kind.
 */
export type ArticleCardProps = {
  item: ContentItem;
  className?: string;
  reveal?: boolean;
  /** Force featured styling even when item.featured is false. */
  featured?: boolean;
};

export function ArticleCard({
  item,
  className,
  reveal = false,
  featured,
}: ArticleCardProps) {
  const isFeatured = featured ?? item.featured ?? false;
  const date = formatPublishDate(item.publishedAt);
  const reading = formatReadingTime(item.readingTimeMinutes);
  const author = item.authors?.[0];

  const card = (
    <Card
      variant={isFeatured ? 'elevated' : 'interactive'}
      className={cn(
        'flex h-full flex-col overflow-hidden',
        isFeatured && 'border-primary/30 ring-primary-muted ring-1',
        className,
      )}
    >
      {item.image ? (
        <div className="border-border border-b">
          <ContentImageView image={item.image} className="rounded-none" />
        </div>
      ) : (
        <div
          aria-hidden="true"
          className="border-border from-primary-muted/40 via-surface-muted to-domain-accent/10 relative h-28 border-b bg-gradient-to-br"
        >
          <div
            className="absolute inset-0 opacity-40"
            style={{
              backgroundImage:
                'linear-gradient(to right, color-mix(in oklab, var(--color-border) 60%, transparent) 1px, transparent 1px), linear-gradient(to bottom, color-mix(in oklab, var(--color-border) 60%, transparent) 1px, transparent 1px)',
              backgroundSize: '1.5rem 1.5rem',
            }}
          />
          <div className="bg-domain-accent/25 absolute right-4 bottom-4 size-2 rounded-full" />
        </div>
      )}
      <CardHeader className="gap-3">
        <Stack direction="horizontal" gap={2} wrap className="w-full">
          {item.category ? <CategoryBadge category={item.category} /> : null}
          {isFeatured ? <Badge variant="primary">Featured</Badge> : null}
          <Badge variant="outline" className="capitalize">
            {item.kind.replaceAll('-', ' ')}
          </Badge>
        </Stack>
        <CardTitle>
          <Link
            href={item.href}
            className="focus-visible:ring-ring hover:text-primary rounded-sm focus-visible:ring-2 focus-visible:outline-none"
          >
            {item.title}
          </Link>
        </CardTitle>
        {item.description ? (
          <CardDescription>{item.description}</CardDescription>
        ) : null}
      </CardHeader>
      <CardContent className="mt-auto">
        <Stack gap={3}>
          <Stack
            direction="horizontal"
            gap={3}
            wrap
            className="text-muted-foreground"
          >
            {date ? (
              <Text as="span" size="caption">
                <time dateTime={item.publishedAt}>{date}</time>
              </Text>
            ) : null}
            {reading ? (
              <Text as="span" size="caption">
                {reading}
              </Text>
            ) : null}
            {author ? (
              <Text as="span" size="caption">
                {author.name}
              </Text>
            ) : null}
          </Stack>
          {item.tags && item.tags.length > 0 ? (
            <ul className="flex flex-wrap gap-1.5">
              {item.tags.map((tag) => (
                <li key={tag.id}>
                  <Badge variant="secondary">{tag.name}</Badge>
                </li>
              ))}
            </ul>
          ) : null}
        </Stack>
      </CardContent>
    </Card>
  );

  if (!reveal) return card;
  return <MarketingReveal enabled>{card}</MarketingReveal>;
}

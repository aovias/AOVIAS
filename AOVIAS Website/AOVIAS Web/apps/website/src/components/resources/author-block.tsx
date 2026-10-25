import Link from 'next/link';

import type { ContentAuthor } from '@/components/content';
import {
  AvatarPlaceholder,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * AuthorBlock — author identity for articles and documentation.
 */
export type AuthorBlockProps = {
  author: ContentAuthor;
  className?: string;
  /** Compact row vs expanded bio layout. */
  variant?: 'compact' | 'detailed';
};

export function AuthorBlock({
  author,
  className,
  variant = 'detailed',
}: AuthorBlockProps) {
  const meta = [author.role, author.organization].filter(Boolean).join(' · ');

  return (
    <aside
      className={cn(
        'border-border flex gap-4',
        variant === 'detailed' && 'rounded-lg border p-4 md:p-5',
        className,
      )}
      aria-label={`Author: ${author.name}`}
    >
      {author.avatar ?? (
        <AvatarPlaceholder
          initials={author.avatarInitials ?? author.name}
          label={author.name}
          size={variant === 'detailed' ? 'lg' : 'md'}
        />
      )}
      <Stack gap={2} className="min-w-0">
        <div>
          <Text as="p" weight="semibold" size="small">
            {author.name}
          </Text>
          {meta ? (
            <Text as="p" size="caption" tone="muted">
              {meta}
            </Text>
          ) : null}
        </div>
        {variant === 'detailed' && author.biography ? (
          <Paragraph tone="muted" size="small">
            {author.biography}
          </Paragraph>
        ) : null}
        {author.socialLinks && author.socialLinks.length > 0 ? (
          <ul className="flex flex-wrap gap-3">
            {author.socialLinks.map((link) => (
              <li key={link.id}>
                <Link
                  href={link.href}
                  className="text-primary text-caption font-medium underline-offset-4 hover:underline"
                  {...(link.external
                    ? { target: '_blank', rel: 'noreferrer' }
                    : {})}
                >
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        ) : null}
      </Stack>
    </aside>
  );
}

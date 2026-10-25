import type { ReactNode } from 'react';

/**
 * Content Domain Model — shared knowledge entities for Blog, Insights,
 * Documentation, Academy, Case Studies, Release Notes, Whitepapers, and more.
 *
 * Resource UI components render these models; do not fork per content type.
 */

export type ContentKind =
  | 'article'
  | 'insight'
  | 'documentation'
  | 'case-study'
  | 'whitepaper'
  | 'release-note'
  | 'academy-lesson'
  | 'resource';

export type ContentCategoryTone =
  | 'default'
  | 'primary'
  | 'secondary'
  | 'info'
  | 'success'
  | 'warning';

export type ContentCategory = {
  id: string;
  name: string;
  slug?: string;
  icon?: ReactNode;
  tone?: ContentCategoryTone;
};

export type ContentTag = {
  id: string;
  name: string;
  slug?: string;
};

export type ContentSocialLink = {
  id: string;
  label: string;
  href: string;
  external?: boolean;
};

export type ContentAuthor = {
  id: string;
  name: string;
  role?: string;
  biography?: string;
  organization?: string;
  avatar?: ReactNode;
  avatarInitials?: string;
  socialLinks?: readonly ContentSocialLink[];
};

export type ContentImage =
  | {
      kind?: 'image';
      src: string;
      alt: string;
      width?: number;
      height?: number;
    }
  | {
      /** Brand abstract cover — preferred for public knowledge cards. */
      kind: 'cover';
      label: string;
      tone?: 'default' | 'primary' | 'secondary' | 'info' | 'success' | 'warning';
      aspect?: 'video' | 'wide' | 'square';
    }
  | {
      /** Reserved for true empty / system states — avoid on public heroes. */
      kind: 'placeholder';
      label: string;
      aspect?: 'video' | 'wide' | 'square';
    };

/**
 * Shared content item — powers cards, grids, and search across knowledge surfaces.
 */
export type ContentItem = {
  id: string;
  kind: ContentKind;
  title: string;
  description?: string;
  href: string;
  category?: ContentCategory;
  tags?: readonly ContentTag[];
  authors?: readonly ContentAuthor[];
  /** ISO-8601 date string. */
  publishedAt?: string;
  updatedAt?: string;
  readingTimeMinutes?: number;
  featured?: boolean;
  image?: ContentImage;
};

/** Article specialization — same shape; kind narrows to article/insight. */
export type Article = ContentItem & {
  kind: 'article' | 'insight';
};

export type DocumentationPage = ContentItem & {
  kind: 'documentation';
  section?: string;
};

export type CaseStudy = ContentItem & { kind: 'case-study' };
export type Whitepaper = ContentItem & { kind: 'whitepaper' };
export type ReleaseNote = ContentItem & {
  kind: 'release-note';
  version?: string;
};
export type AcademyLesson = ContentItem & {
  kind: 'academy-lesson';
  durationMinutes?: number;
};

/** Generic resource alias used by ResourceGrid. */
export type Resource = ContentItem;

export type SearchResult = {
  id: string;
  title: string;
  href: string;
  excerpt: string;
  kind?: ContentKind;
  category?: ContentCategory;
  /** Substrings already identified for highlight rendering. */
  highlights?: readonly string[];
};

export type TocItem = {
  id: string;
  title: string;
  /** Heading level (2–4 recommended for in-article TOC). */
  level: 2 | 3 | 4;
  href: string;
  children?: readonly TocItem[];
};

export type ResourceSortKey = 'publishedAt' | 'title' | 'readingTime';
export type ResourceSortDirection = 'asc' | 'desc';

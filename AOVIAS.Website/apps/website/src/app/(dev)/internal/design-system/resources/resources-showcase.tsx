'use client';

import { useMemo, useState } from 'react';
import { BookOpen } from 'lucide-react';

import type {
  ContentAuthor,
  ContentCategory,
  ContentItem,
  SearchResult,
  TocItem,
} from '@/components/content';
import {
  ArticleCard,
  AuthorBlock,
  CategoryBadge,
  filterContentByKind,
  Pagination,
  ReadingProgress,
  ResourceGrid,
  SearchResults,
  sortContentItems,
  TableOfContents,
} from '@/components/resources';
import {
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';

const engineering: ContentCategory = {
  id: 'engineering',
  name: 'Engineering',
  tone: 'primary',
  icon: <BookOpen />,
};
const security: ContentCategory = {
  id: 'security',
  name: 'Security',
  tone: 'info',
};
const product: ContentCategory = {
  id: 'product',
  name: 'Product',
  tone: 'secondary',
};
const categories: ContentCategory[] = [engineering, security, product];

const author: ContentAuthor = {
  id: 'a1',
  name: 'Sample Author',
  role: 'Principal Engineer',
  organization: 'AOVIAS',
  avatarInitials: 'SA',
  biography:
    'QA biography only — describes how AuthorBlock renders role, organization, and social links.',
  socialLinks: [
    { id: 'li', label: 'LinkedIn', href: '#', external: true },
    { id: 'gh', label: 'GitHub', href: '#', external: true },
  ],
};

const items: ContentItem[] = [
  {
    id: '1',
    kind: 'article',
    title: 'Sample insight on institutional systems',
    description: 'QA card copy for scanning title, description, and metadata.',
    href: '#article-demo',
    featured: true,
    category: engineering,
    tags: [
      { id: 't1', name: 'Architecture' },
      { id: 't2', name: 'Trust' },
    ],
    authors: [author],
    publishedAt: '2026-06-01',
    readingTimeMinutes: 8,
    image: { kind: 'placeholder', label: 'Featured image', aspect: 'video' },
  },
  {
    id: '2',
    kind: 'documentation',
    title: 'Sample documentation topic',
    description: 'Documentation kind in the same ContentItem model.',
    href: '#docs',
    category: product,
    publishedAt: '2026-05-12',
    readingTimeMinutes: 4,
    image: { kind: 'placeholder', label: 'Doc cover', aspect: 'wide' },
  },
  {
    id: '3',
    kind: 'case-study',
    title: 'Sample case study',
    description: 'Case study rendered by ArticleCard without a separate card type.',
    href: '#case',
    category: security,
    publishedAt: '2026-04-20',
    readingTimeMinutes: 12,
    authors: [author],
  },
  {
    id: '4',
    kind: 'release-note',
    title: 'Sample release notes',
    description: 'Release notes share the ResourceGrid layout.',
    href: '#release',
    category: product,
    publishedAt: '2026-07-01',
    readingTimeMinutes: 3,
  },
  {
    id: '5',
    kind: 'whitepaper',
    title: 'Sample whitepaper',
    description: 'Whitepaper entry using ContentItem.',
    href: '#whitepaper',
    category: security,
    publishedAt: '2026-03-08',
    readingTimeMinutes: 18,
  },
  {
    id: '6',
    kind: 'academy-lesson',
    title: 'Sample academy lesson',
    description: 'Academy content uses the same card and grid.',
    href: '#academy',
    category: engineering,
    publishedAt: '2026-02-14',
    readingTimeMinutes: 15,
  },
];

const searchResults: SearchResult[] = [
  {
    id: 's1',
    title: 'Sample insight on institutional systems',
    href: '#article-demo',
    excerpt:
      'QA excerpt showing highlighted keywords inside search results for institutional systems.',
    kind: 'article',
    category: engineering,
    highlights: ['institutional', 'systems'],
  },
  {
    id: 's2',
    title: 'Sample documentation topic',
    href: '#docs',
    excerpt: 'Documentation result with optional category metadata.',
    kind: 'documentation',
    category: product,
    highlights: ['Documentation'],
  },
];

const toc: TocItem[] = [
  {
    id: 'article-demo',
    title: 'Overview',
    level: 2,
    href: '#article-demo',
    children: [
      {
        id: 'reading',
        title: 'Reading experience',
        level: 3,
        href: '#reading',
      },
    ],
  },
  {
    id: 'toc-section',
    title: 'Navigation',
    level: 2,
    href: '#toc-section',
  },
];

function ShowcaseBlock({
  id,
  title,
  notes,
  children,
}: {
  id: string;
  title: string;
  notes?: string;
  children: React.ReactNode;
}) {
  return (
    <section id={id} className="border-border scroll-mt-24 border-b py-12">
      <Stack gap={6}>
        <Stack gap={2}>
          <Heading level={2} size="h2">
            {title}
          </Heading>
          {notes ? <Paragraph tone="muted">{notes}</Paragraph> : null}
        </Stack>
        {children}
      </Stack>
    </section>
  );
}

const nav = [
  { href: '#card', label: 'Article card' },
  { href: '#author', label: 'Author' },
  { href: '#badge', label: 'Category badge' },
  { href: '#grid', label: 'Resource grid' },
  { href: '#search', label: 'Search results' },
  { href: '#pagination', label: 'Pagination' },
  { href: '#reading', label: 'Reading progress' },
  { href: '#toc-section', label: 'Table of contents' },
] as const;

export function ResourcesShowcase() {
  const [activeCategory, setActiveCategory] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [kindFilter, setKindFilter] = useState<ContentItem['kind'][]>([]);

  const filtered = useMemo(() => {
    let next = sortContentItems(items, 'publishedAt', 'desc');
    if (kindFilter.length > 0) next = filterContentByKind(next, kindFilter);
    if (activeCategory) {
      next = next.filter((item) => item.category?.id === activeCategory);
    }
    return next;
  }, [activeCategory, kindFilter]);

  return (
    <div>
      <ReadingProgress className="mb-6" />

      <nav
        aria-label="Resource components"
        className="border-border mb-8 border-b pb-6"
      >
        <ul className="flex flex-wrap gap-x-4 gap-y-2">
          {nav.map((item) => (
            <li key={item.href}>
              <a
                href={item.href}
                className="text-muted-foreground hover:text-foreground text-small underline-offset-4 hover:underline"
              >
                {item.label}
              </a>
            </li>
          ))}
        </ul>
      </nav>

      <ShowcaseBlock
        id="card"
        title="Article Card"
        notes="Renders any ContentItem kind — featured variant, tags, metadata."
      >
        <div className="max-w-md">
          <ArticleCard item={items[0]!} featured />
        </div>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="author"
        title="Author Block"
        notes="Compact and detailed variants from ContentAuthor."
      >
        <div className="grid gap-4 md:grid-cols-2">
          <AuthorBlock author={author} variant="detailed" />
          <AuthorBlock author={author} variant="compact" />
        </div>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="badge"
        title="Category Badge"
        notes="Tone variants and active filter chips."
      >
        <Stack direction="horizontal" gap={2} wrap>
          {categories.map((category) => (
            <CategoryBadge
              key={category.id}
              category={category}
              active={activeCategory === category.id}
              onClick={() =>
                setActiveCategory((current) =>
                  current === category.id ? null : category.id,
                )
              }
            />
          ))}
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="grid"
        title="Resource Grid"
        notes="Filter/sort ready — parent applies helpers, grid stays presentational."
      >
        <Stack direction="horizontal" gap={2} wrap className="mb-4">
          {(
            [
              'article',
              'documentation',
              'case-study',
              'release-note',
            ] as const
          ).map((kind) => (
            <CategoryBadge
              key={kind}
              category={{
                id: kind,
                name: kind.replace('-', ' '),
                tone: 'secondary',
              }}
              active={kindFilter.includes(kind)}
              onClick={() =>
                setKindFilter((current) =>
                  current.includes(kind)
                    ? current.filter((item) => item !== kind)
                    : [...current, kind],
                )
              }
            />
          ))}
        </Stack>
        <ResourceGrid items={filtered} cols={3} reveal={false} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="search"
        title="Search Results"
        notes="Highlighted keywords, metadata, and empty state."
      >
        <SearchResults
          results={searchResults}
          query="institutional"
          reveal={false}
        />
        <div className="mt-6">
          <SearchResults results={[]} query="zzzz" reveal={false} />
        </div>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="pagination"
        title="Pagination"
        notes="Desktop page numbers; mobile shows page status with prev/next."
      >
        <Pagination
          currentPage={page}
          totalPages={8}
          onPageChange={setPage}
        />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="reading"
        title="Reading Progress"
        notes="Sticky indicator at the top of this gallery tracks page scroll."
      >
        <Text as="p" size="small" tone="muted">
          Scroll the page to observe the progress bar above the component nav.
        </Text>
        <div className="space-y-4 pt-4">
          {Array.from({ length: 6 }).map((_, index) => (
            <Paragraph key={index} tone="muted">
              Sample long-form paragraph {index + 1} for reading progress QA.
              Content is placeholder only and not editorial copy.
            </Paragraph>
          ))}
        </div>
      </ShowcaseBlock>

      <ShowcaseBlock
        id="toc-section"
        title="Table of Contents"
        notes="Nested headings with sticky desktop behaviour and active tracking."
      >
        <div className="grid gap-8 lg:grid-cols-[16rem_1fr]">
          <TableOfContents items={toc} />
          <article id="article-demo" className="space-y-8">
            <div>
              <Heading level={3} size="h3">
                Overview
              </Heading>
              <Paragraph tone="muted" className="mt-2">
                Sample section used by the TOC observer. QA content only.
              </Paragraph>
            </div>
            <div id="reading">
              <Heading level={3} size="h3">
                Reading experience
              </Heading>
              <Paragraph tone="muted" className="mt-2">
                Nested heading demonstrates TOC children and active states.
              </Paragraph>
            </div>
          </article>
        </div>
      </ShowcaseBlock>
    </div>
  );
}

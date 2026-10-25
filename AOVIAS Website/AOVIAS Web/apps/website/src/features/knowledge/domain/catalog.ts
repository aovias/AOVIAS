import type { ContentImage, ContentItem } from '@/components/content';

const cover = (
  label: string,
  tone: NonNullable<Extract<ContentImage, { kind: 'cover' }>['tone']> = 'default',
): ContentImage => ({
  kind: 'cover',
  label,
  tone,
  aspect: 'video',
});

const categories = {
  engineering: {
    id: 'engineering',
    name: 'Engineering',
    slug: 'engineering',
    tone: 'primary' as const,
  },
  platform: {
    id: 'platform',
    name: 'Platform',
    slug: 'platform',
    tone: 'secondary' as const,
  },
  governance: {
    id: 'governance',
    name: 'Governance',
    slug: 'governance',
    tone: 'info' as const,
  },
  operations: {
    id: 'operations',
    name: 'Operations',
    slug: 'operations',
    tone: 'success' as const,
  },
  guides: {
    id: 'guides',
    name: 'Product Guides',
    slug: 'guides',
    tone: 'info' as const,
  },
  docs: {
    id: 'docs',
    name: 'Documentation',
    slug: 'docs',
    tone: 'secondary' as const,
  },
} as const;

/**
 * AOVIAS Knowledge catalog — single source of truth for Blog, Insights,
 * Documentation, Resources, Search, and Categories.
 */
export const knowledgeCatalog: readonly ContentItem[] = [
  {
    id: 'blog-institutional-trust',
    kind: 'article',
    title: 'Designing technology for institutional trust',
    description:
      'How clear ownership, measured claims, and durable architecture become product requirements for institutions that society depends on.',
    href: '/resources/blog/institutional-trust',
    publishedAt: '2026-06-12',
    readingTimeMinutes: 10,
    featured: true,
    category: categories.engineering,
    tags: [
      { id: 'trust', name: 'Trust', slug: 'trust' },
      { id: 'engineering', name: 'Engineering', slug: 'engineering' },
    ],
    authors: [
      {
        id: 'aovias-engineering',
        name: 'AOVIAS Engineering',
        role: 'Engineering',
        organization: 'AOVIAS',
        avatarInitials: 'AE',
      },
    ],
    image: cover('Institutional trust', 'primary'),
  },
  {
    id: 'blog-release-discipline',
    kind: 'article',
    title: 'Release discipline without artificial urgency',
    description:
      'A calm approach to shipping institutional software — continuity first, spectacle never, ownership always visible.',
    href: '/resources/blog/release-discipline',
    publishedAt: '2026-05-20',
    readingTimeMinutes: 8,
    category: categories.engineering,
    tags: [{ id: 'delivery', name: 'Delivery', slug: 'delivery' }],
    authors: [
      {
        id: 'aovias-engineering',
        name: 'AOVIAS Engineering',
        role: 'Engineering',
        organization: 'AOVIAS',
        avatarInitials: 'AE',
      },
    ],
    image: cover('Release discipline', 'primary'),
  },
  {
    id: 'insight-responsible-ai',
    kind: 'insight',
    title: 'Responsible AI in regulated environments',
    description:
      'Principles for evaluating intelligence capabilities where accountability cannot be deferred and human judgment must remain visible.',
    href: '/resources/insights/responsible-ai',
    publishedAt: '2026-06-01',
    readingTimeMinutes: 11,
    featured: true,
    category: categories.governance,
    tags: [
      { id: 'ai', name: 'AI', slug: 'ai' },
      { id: 'governance', name: 'Governance', slug: 'governance' },
    ],
    authors: [
      {
        id: 'aovias-insights',
        name: 'AOVIAS Insights',
        role: 'Research',
        organization: 'AOVIAS',
        avatarInitials: 'AI',
      },
    ],
    image: cover('Responsible AI', 'info'),
  },
  {
    id: 'insight-operational-clarity',
    kind: 'insight',
    title: 'Operational clarity as institutional advantage',
    description:
      'Why structured visibility and attributable ownership outperform dashboard theater in complex institutions.',
    href: '/resources/insights/operational-clarity',
    publishedAt: '2026-04-18',
    readingTimeMinutes: 9,
    category: categories.operations,
    tags: [{ id: 'operations', name: 'Operations', slug: 'operations' }],
    authors: [
      {
        id: 'aovias-insights',
        name: 'AOVIAS Insights',
        role: 'Research',
        organization: 'AOVIAS',
        avatarInitials: 'AI',
      },
    ],
    image: cover('Operational clarity', 'success'),
  },
  {
    id: 'doc-platform-overview',
    kind: 'documentation',
    title: 'Platform overview',
    description:
      'Orientation for teams exploring AOVIAS products, how Command and Studio relate, and where to begin evaluation.',
    href: '/resources/docs/platform-overview',
    publishedAt: '2026-05-28',
    updatedAt: '2026-07-01',
    readingTimeMinutes: 7,
    featured: true,
    category: categories.docs,
    tags: [{ id: 'platform', name: 'Platform', slug: 'platform' }],
    image: cover('Platform overview', 'secondary'),
  },
  {
    id: 'doc-command-getting-started',
    kind: 'documentation',
    title: 'Getting started with AOVIAS Command',
    description:
      'Guidance for evaluating and adopting Command so operators gain structured visibility and accountable control.',
    href: '/resources/docs/command-getting-started',
    publishedAt: '2026-06-08',
    readingTimeMinutes: 8,
    category: categories.docs,
    tags: [
      { id: 'command', name: 'Command', slug: 'command' },
      { id: 'implementation', name: 'Implementation', slug: 'implementation' },
    ],
    image: cover('Command getting started', 'secondary'),
  },
  {
    id: 'doc-studio-delivery',
    kind: 'documentation',
    title: 'Studio delivery practices',
    description:
      'How teams use AOVIAS Studio to keep solution design and delivery coherent across ownership changes.',
    href: '/resources/docs/studio-delivery',
    publishedAt: '2026-06-15',
    readingTimeMinutes: 8,
    category: categories.docs,
    tags: [{ id: 'studio', name: 'Studio', slug: 'studio' }],
    image: cover('Studio delivery', 'secondary'),
  },
  {
    id: 'resource-ai-adoption-guide',
    kind: 'resource',
    title: 'A practical guide to responsible AI adoption',
    description:
      'A measured framework for evaluating AI capabilities inside regulated and high-stakes institutional environments.',
    href: '/resources/library/responsible-ai-guide',
    publishedAt: '2026-04-10',
    readingTimeMinutes: 10,
    featured: true,
    category: categories.guides,
    tags: [
      { id: 'ai', name: 'AI', slug: 'ai' },
      { id: 'guide', name: 'Guide', slug: 'guide' },
    ],
    image: cover('AI adoption guide', 'info'),
  },
  {
    id: 'resource-evaluation-checklist',
    kind: 'resource',
    title: 'Enterprise evaluation checklist',
    description:
      'Reference questions for security, governance, interoperability, and operational readiness when assessing AOVIAS.',
    href: '/resources/library/evaluation-checklist',
    publishedAt: '2026-03-22',
    readingTimeMinutes: 7,
    category: categories.guides,
    tags: [{ id: 'evaluation', name: 'Evaluation', slug: 'evaluation' }],
    image: cover('Evaluation checklist', 'info'),
  },
  {
    id: 'release-2026-07',
    kind: 'release-note',
    title: 'Platform notes — July 2026',
    description:
      'Measured updates across Command and Studio documentation and public website foundations.',
    href: '/resources/library/platform-notes-2026-07',
    publishedAt: '2026-07-15',
    readingTimeMinutes: 4,
    category: categories.platform,
    tags: [{ id: 'releases', name: 'Releases', slug: 'releases' }],
    image: cover('Platform notes', 'secondary'),
  },
] as const;

export function getKnowledgeItemBySlug(
  kindSegment: string,
  slug: string,
): ContentItem | undefined {
  const href = `/resources/${kindSegment}/${slug}`;
  return knowledgeCatalog.find((item) => item.href === href);
}

export function getKnowledgeItemById(id: string): ContentItem | undefined {
  return knowledgeCatalog.find((item) => item.id === id);
}

export function getKnowledgeByKind(
  kinds: readonly ContentItem['kind'][],
): ContentItem[] {
  const set = new Set(kinds);
  return knowledgeCatalog
    .filter((item) => set.has(item.kind))
    .sort((a, b) => {
      const aTime = a.publishedAt ? Date.parse(a.publishedAt) : 0;
      const bTime = b.publishedAt ? Date.parse(b.publishedAt) : 0;
      return bTime - aTime;
    });
}

export function getFeaturedKnowledge(): ContentItem[] {
  return knowledgeCatalog.filter((item) => item.featured);
}

export function getKnowledgeByCategorySlug(categorySlug: string): ContentItem[] {
  return knowledgeCatalog.filter((item) => item.category?.slug === categorySlug);
}

export function getRelatedKnowledge(
  item: ContentItem,
  limit = 3,
  relatedIds?: readonly string[],
): ContentItem[] {
  if (relatedIds?.length) {
    return relatedIds
      .map((id) => getKnowledgeItemById(id))
      .filter((candidate): candidate is ContentItem => Boolean(candidate))
      .filter((candidate) => candidate.id !== item.id)
      .slice(0, limit);
  }

  return knowledgeCatalog
    .filter((candidate) => {
      if (candidate.id === item.id) return false;
      if (candidate.kind === item.kind) return true;
      if (
        item.category?.id &&
        candidate.category?.id &&
        candidate.category.id === item.category.id
      ) {
        return true;
      }
      return false;
    })
    .slice(0, limit);
}

export function getAdjacentKnowledge(
  items: readonly ContentItem[],
  currentId: string,
): { previous?: ContentItem; next?: ContentItem } {
  const index = items.findIndex((item) => item.id === currentId);
  if (index < 0) return {};
  return {
    ...(index > 0 ? { previous: items[index - 1] } : {}),
    ...(index < items.length - 1 ? { next: items[index + 1] } : {}),
  };
}

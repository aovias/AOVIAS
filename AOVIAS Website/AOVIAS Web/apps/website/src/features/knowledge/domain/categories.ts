import type { ContentCategory } from '@/components/content';

/**
 * Shared knowledge categories — used for navigation, filtering, and category pages.
 * Public names stay plain; avoid meta language about scaffolding or content systems.
 */
export const knowledgeCategories: readonly ContentCategory[] = [
  {
    id: 'engineering',
    name: 'Engineering',
    slug: 'engineering',
    tone: 'primary',
  },
  {
    id: 'platform',
    name: 'Platform',
    slug: 'platform',
    tone: 'secondary',
  },
  {
    id: 'governance',
    name: 'Governance',
    slug: 'governance',
    tone: 'info',
  },
  {
    id: 'operations',
    name: 'Operations',
    slug: 'operations',
    tone: 'success',
  },
  {
    id: 'guides',
    name: 'Product Guides',
    slug: 'guides',
    tone: 'info',
  },
  {
    id: 'docs',
    name: 'Documentation',
    slug: 'docs',
    tone: 'secondary',
  },
] as const;

/** Short public intros for category list pages. */
export const knowledgeCategoryIntros: Record<string, string> = {
  engineering:
    'Articles and notes on engineering discipline, release practice, and building systems institutions can trust.',
  platform:
    'Updates and orientation across the AOVIAS platform — products, documentation, and measured progress.',
  governance:
    'Research and guidance on accountability, responsible AI, security posture, and oversight in regulated environments.',
  operations:
    'Practical writing on operational clarity, ownership, and visibility for teams that run institutional work.',
  guides:
    'Product guides and checklists for evaluation and adoption — written for diligence, not promotion.',
  docs: 'Product and implementation documentation for Command, Studio, and platform orientation.',
};

export function getCategoryBySlug(slug: string): ContentCategory | undefined {
  return knowledgeCategories.find((category) => category.slug === slug);
}

export function getCategoryIntro(slug: string, fallbackName: string): string {
  return (
    knowledgeCategoryIntros[slug] ??
    `Knowledge grouped under ${fallbackName} across blog, insights, documentation, and resources.`
  );
}

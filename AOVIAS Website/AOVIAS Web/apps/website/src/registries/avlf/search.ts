import { companyItems } from '@/config/navigation/company';
import { productGroups } from '@/config/navigation/products';
import { resourceItems } from '@/config/navigation/resources';
import { solutionGroups } from '@/config/navigation/solutions';
import { technologyFooterItems } from '@/config/navigation/technology';
import type { NavItem } from '@/config/navigation/types';

/**
 * Enterprise Search categories (AVLS / GEI).
 * Architecture supports Release 2 provider swap via SearchProvider.
 */
export const searchCategories = [
  'products',
  'technology',
  'industries',
  'resources',
  'company',
  'legal',
  'pricing',
  'contact',
] as const;

export type SearchCategory = (typeof searchCategories)[number];

export type EnterpriseSearchItem = {
  id: string;
  title: string;
  href: string;
  category: SearchCategory;
  description?: string;
  keywords?: readonly string[];
};

export type EnterpriseSearchResult = EnterpriseSearchItem & {
  score: number;
};

export type SearchProvider = {
  readonly id: string;
  search(query: string): EnterpriseSearchResult[] | Promise<EnterpriseSearchResult[]>;
};

const legalItems: readonly NavItem[] = [
  { id: 'hub', label: 'Trust & Governance', href: '/legal' },
  { id: 'privacy', label: 'Privacy', href: '/legal/privacy' },
  { id: 'terms', label: 'Terms', href: '/legal/terms' },
  { id: 'cookies', label: 'Cookies', href: '/legal/cookies' },
  { id: 'accessibility', label: 'Accessibility', href: '/legal/accessibility' },
  { id: 'security', label: 'Security', href: '/legal/security' },
] as const;

function fromNavItems(
  items: readonly NavItem[],
  category: SearchCategory,
  prefix: string,
): EnterpriseSearchItem[] {
  return items
    .filter((item) => !item.hidden)
    .map((item) => ({
      id: `${prefix}-${item.id}`,
      title: item.label,
      href: item.href,
      category,
      ...(item.description ? { description: item.description } : {}),
      keywords: [item.label, category, ...(item.description ? [item.description] : [])],
    }));
}

function buildStaticRegistry(): readonly EnterpriseSearchItem[] {
  const industryItems =
    solutionGroups.find((group) => group.id === 'industries')?.items ?? [];

  const productItems = productGroups.flatMap((group) => group.items);

  return [
    ...fromNavItems(productItems, 'products', 'product'),
    {
      id: 'products-hub',
      title: 'Products',
      href: '/products',
      category: 'products',
      description: 'AOVIAS product suite overview',
      keywords: ['products', 'suite', 'studio', 'command'],
    },
    ...fromNavItems(technologyFooterItems, 'technology', 'technology'),
    {
      id: 'technology-hub',
      title: 'Technology',
      href: '/technology',
      category: 'technology',
      description: 'Platform, engineering, AI, and trust',
      keywords: ['technology', 'platform', 'engineering', 'ai'],
    },
    ...fromNavItems(industryItems, 'industries', 'industry'),
    {
      id: 'solutions-hub',
      title: 'Solutions & Industries',
      href: '/solutions',
      category: 'industries',
      description: 'Industry experiences powered by AOVIAS',
      keywords: ['solutions', 'industries'],
    },
    ...fromNavItems(resourceItems, 'resources', 'resource'),
    ...fromNavItems(companyItems, 'company', 'company'),
    ...fromNavItems(legalItems, 'legal', 'legal'),
    {
      id: 'pricing',
      title: 'Pricing',
      href: '/pricing',
      category: 'pricing',
      description: 'Studio and Command commercial options',
      keywords: ['pricing', 'plans', 'commercial'],
    },
    {
      id: 'contact',
      title: 'Contact',
      href: '/company/contact',
      category: 'contact',
      description: 'Speak with the AOVIAS team',
      keywords: ['contact', 'advisory', 'partner', 'sales'],
    },
  ] as const;
}

/** Static enterprise search index — Release 1.1 GEI. */
export const enterpriseSearchRegistry: readonly EnterpriseSearchItem[] =
  buildStaticRegistry();

const categoryLabel: Record<SearchCategory, string> = {
  products: 'Products',
  technology: 'Technology',
  industries: 'Industries',
  resources: 'Resources',
  company: 'Company',
  legal: 'Legal',
  pricing: 'Pricing',
  contact: 'Contact',
};

export function getSearchCategoryLabel(category: SearchCategory): string {
  return categoryLabel[category];
}

function scoreItem(item: EnterpriseSearchItem, query: string): number {
  const q = query.trim().toLowerCase();
  if (!q) return 0;

  const title = item.title.toLowerCase();
  const description = (item.description ?? '').toLowerCase();
  const keywords = (item.keywords ?? []).join(' ').toLowerCase();

  if (title === q) return 100;
  if (title.startsWith(q)) return 80;
  if (title.includes(q)) return 60;
  if (keywords.includes(q)) return 40;
  if (description.includes(q)) return 30;

  const tokens = q.split(/\s+/).filter(Boolean);
  const tokenHits = tokens.filter(
    (token) =>
      title.includes(token) || keywords.includes(token) || description.includes(token),
  ).length;
  if (tokenHits === 0) return 0;
  return Math.round((tokenHits / tokens.length) * 25);
}

/**
 * Static search provider — default for Release 1.1.
 * Release 2 may inject a remote/indexed provider with the same interface.
 */
export const staticSearchProvider: SearchProvider = {
  id: 'static-registry-v1',
  search(query: string): EnterpriseSearchResult[] {
    const q = query.trim();
    if (!q) {
      return enterpriseSearchRegistry.map((item) => ({ ...item, score: 0 }));
    }

    return enterpriseSearchRegistry
      .map((item) => ({ ...item, score: scoreItem(item, q) }))
      .filter((item) => item.score > 0)
      .sort((a, b) => b.score - a.score || a.title.localeCompare(b.title));
  },
};

let activeSearchProvider: SearchProvider = staticSearchProvider;

/** Swap search backend (Release 2). Defaults to static registry. */
export function setSearchProvider(provider: SearchProvider): void {
  activeSearchProvider = provider;
}

export function getSearchProvider(): SearchProvider {
  return activeSearchProvider;
}

export async function runEnterpriseSearch(
  query: string,
): Promise<EnterpriseSearchResult[]> {
  return activeSearchProvider.search(query);
}

export function groupSearchResults(
  results: readonly EnterpriseSearchResult[],
): { category: SearchCategory; label: string; items: EnterpriseSearchResult[] }[] {
  const order = searchCategories;
  return order
    .map((category) => {
      const items = results.filter((item) => item.category === category);
      return {
        category,
        label: getSearchCategoryLabel(category),
        items,
      };
    })
    .filter((group) => group.items.length > 0);
}

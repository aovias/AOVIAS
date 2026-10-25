import {
  paginateArray,
  sdkOk,
  type KnowledgeArticle,
  type KnowledgeClient,
  type KnowledgeKind,
} from '@aovias/sdk';

import type { ContentItem } from '@/components/content';
import { knowledgeCatalog } from '@/features/knowledge/domain/catalog';
import { knowledgeCategories } from '@/features/knowledge/domain/categories';
import { searchKnowledge } from '@/features/knowledge/domain/search';
import { mockDelay } from '@/mocks/delay';

function toArticle(item: ContentItem): KnowledgeArticle {
  const slug =
    item.href.split('/').filter(Boolean).at(-1) ?? item.id;
  return {
    id: item.id,
    slug,
    title: item.title,
    ...(item.description ? { description: item.description } : {}),
    kind: item.kind as KnowledgeKind,
    href: item.href,
    ...(item.category?.slug
      ? {
          category: {
            slug: item.category.slug,
            name: item.category.name,
          },
        }
      : {}),
    ...(item.tags ? { tags: item.tags.map((tag) => tag.name) } : {}),
    ...(item.publishedAt ? { publishedAt: item.publishedAt } : {}),
    ...(item.featured !== undefined ? { featured: item.featured } : {}),
  };
}

/**
 * Mock Knowledge SDK — loads from static TypeScript catalog only.
 */
export function createMockKnowledgeClient(): KnowledgeClient {
  return {
    async listArticles(request = {}) {
      await mockDelay(150);
      let items = knowledgeCatalog.map(toArticle);
      if (request.kind) {
        items = items.filter((a) => a.kind === request.kind);
      }
      if (request.categorySlug) {
        items = items.filter((a) => a.category?.slug === request.categorySlug);
      }
      if (request.featuredOnly) {
        items = items.filter((a) => a.featured);
      }
      return sdkOk(
        paginateArray(items, {
          ...(request.page !== undefined ? { page: request.page } : {}),
          ...(request.pageSize !== undefined
            ? { pageSize: request.pageSize }
            : {}),
        }),
      );
    },

    async getArticle(slug) {
      await mockDelay(100);
      const item = knowledgeCatalog.find((a) => {
        const article = toArticle(a);
        return article.slug === slug || a.id === slug;
      });
      return sdkOk(item ? toArticle(item) : null);
    },

    async listCategories() {
      await mockDelay(100);
      return sdkOk(
        knowledgeCategories.map((c) => ({
          slug: c.slug ?? c.id,
          name: c.name,
        })),
      );
    },

    async listFeatured(limit = 6) {
      await mockDelay(100);
      const featured = knowledgeCatalog
        .filter((item) => item.featured)
        .slice(0, limit)
        .map(toArticle);
      return sdkOk(featured);
    },

    async getRelated(slug, limit = 4) {
      await mockDelay(100);
      const current = knowledgeCatalog.find((a) => {
        const article = toArticle(a);
        return article.slug === slug || a.id === slug;
      });
      if (!current) return sdkOk([]);
      const related = knowledgeCatalog
        .filter(
          (item) =>
            item.id !== current.id &&
            (item.kind === current.kind ||
              item.category?.slug === current.category?.slug),
        )
        .slice(0, limit)
        .map(toArticle);
      return sdkOk(related);
    },

    async search({ query, kind, categorySlug }) {
      await mockDelay(150);
      const hits = searchKnowledge({
        query,
        ...(kind ? { kinds: [kind] } : {}),
        ...(categorySlug ? { categorySlug } : {}),
      }).map((hit) => {
        const source = knowledgeCatalog.find((a) => a.id === hit.id);
        return {
          article: source
            ? toArticle(source)
            : {
                id: hit.id,
                slug: hit.id,
                title: hit.title,
                kind: 'article' as const,
                href: hit.href,
              },
          score: 1,
          highlights: hit.highlights ?? [],
        };
      });
      return sdkOk(hits);
    },

    getClientInfo() {
      return { name: 'knowledge', mode: 'mock', version: '0.1.0' };
    },
  };
}

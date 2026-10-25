import { sdkOk, type SearchClient, type SearchResultItem } from '@aovias/sdk';

import { knowledgeCatalog } from '@/features/knowledge/domain/catalog';
import { searchKnowledge } from '@/features/knowledge/domain/search';
import { technologyGroups } from '@/features/technology/domain/registry';
import { mockDelay } from '@/mocks/delay';

/**
 * Mock Search SDK — local indexes over knowledge + technology IA.
 */
export function createMockSearchClient(): SearchClient {
  return {
    async search({ query, scopes, limit = 20 }) {
      await mockDelay(200);
      const q = query.trim();
      const results: SearchResultItem[] = [];
      const activeScopes = scopes && scopes.length > 0 ? new Set(scopes) : null;

      if (!activeScopes || activeScopes.has('knowledge') || activeScopes.has('all')) {
        for (const hit of searchKnowledge({ query: q }).slice(0, limit)) {
          results.push({
            id: hit.id,
            title: hit.title,
            ...(hit.excerpt ? { description: hit.excerpt } : {}),
            href: hit.href,
            scope: 'knowledge',
            ...(hit.highlights ? { highlights: hit.highlights } : {}),
          });
        }
      }

      if (!activeScopes || activeScopes.has('technology') || activeScopes.has('all')) {
        for (const group of technologyGroups) {
          for (const topic of group.topics) {
            const hay = `${topic.label} ${topic.description}`.toLowerCase();
            if (!q || hay.includes(q.toLowerCase())) {
              const href =
                topic.hrefOverride ??
                (topic.slug ? `/technology/${topic.slug}` : '/technology');
              results.push({
                id: topic.id,
                title: topic.label,
                description: topic.description,
                href,
                scope: 'technology',
              });
            }
          }
        }
      }

      const sliced = results.slice(0, limit);
      const suggestions = sliced.slice(0, 5).map((item) => ({
        id: `sug-${item.id}`,
        label: item.title,
        href: item.href,
        scope: item.scope,
      }));

      return sdkOk({
        query: q,
        results: sliced,
        suggestions,
        total: sliced.length,
      });
    },

    async suggest({ query, limit = 5 }) {
      await mockDelay(100);
      const q = query.trim().toLowerCase();
      const suggestions = knowledgeCatalog
        .filter(
          (item) =>
            !q ||
            item.title.toLowerCase().includes(q) ||
            (item.description?.toLowerCase().includes(q) ?? false),
        )
        .slice(0, limit)
        .map((item) => ({
          id: item.id,
          label: item.title,
          href: item.href,
          scope: 'knowledge' as const,
        }));
      return sdkOk(suggestions);
    },

    getClientInfo() {
      return { name: 'search', mode: 'mock', version: '0.1.0' };
    },
  };
}

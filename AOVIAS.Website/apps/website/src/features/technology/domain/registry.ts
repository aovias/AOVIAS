import type { TechnologyGroup, TechnologyTopic } from './types';

/**
 * Single source of truth for Technology IA — navigation, routes, sitemap,
 * landing cards, and placeholders all derive from this registry.
 */
export const technologyGroups: readonly TechnologyGroup[] = [
  {
    id: 'platform',
    label: 'Platform',
    description: 'Shared foundation, architecture, and delivery services',
    topics: [
      {
        id: 'platform-hub',
        label: 'AOVIAS Platform',
        description: 'Enterprise platform vision and principles',
        groupId: 'platform',
        status: 'live',
        icon: 'layers',
        hrefOverride: '/technology/platform',
      },
      {
        id: 'platform-overview',
        label: 'Platform Overview',
        description: 'Platform-first philosophy and shared capability model',
        groupId: 'platform',
        status: 'live',
        icon: 'box',
        hrefOverride: '/technology/platform/overview',
      },
      {
        id: 'platform-services',
        label: 'Platform Services',
        description: 'Shared services that power AOVIAS products',
        groupId: 'platform',
        status: 'live',
        icon: 'layers',
        hrefOverride: '/technology/platform/services',
      },
      {
        id: 'platform-ecosystem',
        label: 'Platform Ecosystem',
        description: 'How platform, products, institutions, and society connect',
        groupId: 'platform',
        status: 'live',
        icon: 'users',
        hrefOverride: '/technology/platform/ecosystem',
      },
      {
        id: 'architecture',
        slug: 'architecture',
        label: 'Architecture',
        description: 'Enterprise architecture principles and system shape',
        groupId: 'platform',
        status: 'live',
        icon: 'box',
      },
    ],
  },
  {
    id: 'intelligence',
    label: 'Intelligence',
    description: 'Responsible use of intelligence as a platform capability',
    topics: [
      {
        id: 'ai',
        label: 'AI at AOVIAS',
        description: 'How intelligence fits the institutional platform',
        groupId: 'intelligence',
        status: 'live',
        icon: 'search',
        hrefOverride: '/technology/ai',
      },
      {
        id: 'ai-principles',
        label: 'AI Principles',
        description: 'Principles that guide responsible capability design',
        groupId: 'intelligence',
        status: 'live',
        icon: 'file',
        hrefOverride: '/technology/ai/principles',
      },
      {
        id: 'ai-capabilities',
        label: 'AI Capabilities',
        description: 'Capabilities available across the platform',
        groupId: 'intelligence',
        status: 'live',
        icon: 'box',
        hrefOverride: '/technology/ai/capabilities',
      },
      {
        id: 'ai-hitl',
        label: 'Human-in-the-Loop',
        description: 'AI proposes; people review, approve, and stay accountable',
        groupId: 'intelligence',
        status: 'live',
        icon: 'users',
        hrefOverride: '/technology/ai/human-in-the-loop',
      },
      {
        id: 'responsible-ai',
        label: 'Responsible AI',
        description: 'Governance, oversight, and accountable use',
        groupId: 'intelligence',
        status: 'live',
        icon: 'shield',
        hrefOverride: '/technology/ai/responsible',
      },
      {
        id: 'ai-roadmap',
        label: 'AI Roadmap',
        description: 'Capability themes without speculative commitments',
        groupId: 'intelligence',
        status: 'live',
        icon: 'arrowRight',
        hrefOverride: '/technology/ai/roadmap',
      },
    ],
  },
  {
    id: 'engineering',
    label: 'Engineering',
    description: 'Discipline, architecture, and operational excellence',
    topics: [
      {
        id: 'engineering-hub',
        label: 'Engineering Philosophy',
        description: 'How AOVIAS engineers software institutions can depend on',
        groupId: 'engineering',
        status: 'live',
        icon: 'layers',
        hrefOverride: '/technology/engineering',
      },
      {
        id: 'engineering-architecture',
        label: 'Architecture',
        description: 'Platform-first structure and long-term evolution',
        groupId: 'engineering',
        status: 'live',
        icon: 'box',
        hrefOverride: '/technology/engineering/architecture',
      },
      {
        id: 'engineering-design-principles',
        label: 'Design Principles',
        description: 'Principles that guide product and engineering decisions',
        groupId: 'engineering',
        status: 'live',
        icon: 'file',
        hrefOverride: '/technology/engineering/design-principles',
      },
      {
        id: 'engineering-reliability',
        label: 'Reliability',
        description: 'Dependable software as an engineering feature',
        groupId: 'engineering',
        status: 'live',
        icon: 'shield',
        hrefOverride: '/technology/engineering/reliability',
      },
      {
        id: 'engineering-scalability',
        label: 'Scalability',
        description: 'Growth through sustainable architecture',
        groupId: 'engineering',
        status: 'live',
        icon: 'arrowRight',
        hrefOverride: '/technology/engineering/scalability',
      },
      {
        id: 'engineering-observability',
        label: 'Observability',
        description: 'Visibility that enables reliable operations',
        groupId: 'engineering',
        status: 'live',
        icon: 'search',
        hrefOverride: '/technology/engineering/observability',
      },
    ],
  },
  {
    id: 'trust',
    label: 'Trust',
    description: 'Security, privacy, identity, and stewardship foundations',
    topics: [
      {
        id: 'security',
        label: 'Security',
        description: 'Security as a continuous engineering practice',
        groupId: 'trust',
        status: 'live',
        icon: 'shield',
        hrefOverride: '/technology/trust',
      },
      {
        id: 'privacy',
        label: 'Privacy & Trust',
        description: 'Stewardship, transparency, and privacy by design',
        groupId: 'trust',
        status: 'live',
        icon: 'users',
        hrefOverride: '/technology/trust/privacy',
      },
      {
        id: 'identity',
        label: 'Identity Platform',
        description: 'Shared authentication and authorization capability',
        groupId: 'trust',
        status: 'live',
        icon: 'box',
        hrefOverride: '/technology/trust/identity',
      },
      {
        id: 'compliance',
        label: 'Compliance Readiness',
        description: 'Support for regulated industries — without certification claims',
        groupId: 'trust',
        status: 'live',
        icon: 'file',
        hrefOverride: '/technology/trust/compliance',
      },
      {
        id: 'data-protection',
        label: 'Data Protection',
        description: 'Protection principles across the information lifecycle',
        groupId: 'trust',
        status: 'live',
        icon: 'layers',
        hrefOverride: '/technology/trust/data-protection',
      },
    ],
  },
  {
    id: 'ecosystem',
    label: 'Ecosystem',
    description: 'Integrations, APIs, developer vision, and roadmap',
    topics: [
      {
        id: 'integrations',
        label: 'Integrations',
        description: 'Connecting institutional systems with clarity',
        groupId: 'ecosystem',
        status: 'live',
        icon: 'layers',
        hrefOverride: '/technology/integrations',
      },
      {
        id: 'apis',
        label: 'Platform APIs (Future)',
        description: 'API-first philosophy — documentation forthcoming',
        groupId: 'ecosystem',
        status: 'live',
        icon: 'file',
        hrefOverride: '/technology/integrations/apis',
      },
      {
        id: 'developer',
        label: 'Developer Platform (Future)',
        description: 'Strategic direction for institutional builders',
        groupId: 'ecosystem',
        status: 'live',
        icon: 'box',
        hrefOverride: '/technology/developer',
      },
      {
        id: 'roadmap',
        label: 'Technology Roadmap',
        description: 'Capability evolution without release commitments',
        groupId: 'ecosystem',
        status: 'live',
        icon: 'arrowRight',
        hrefOverride: '/technology/roadmap',
      },
      {
        id: 'why-technology',
        label: 'Why AOVIAS Technology',
        description: 'Executive summary of the Technology pillar',
        groupId: 'ecosystem',
        status: 'live',
        icon: 'book',
        hrefOverride: '/technology/why',
      },
      {
        id: 'documentation',
        label: 'Documentation',
        description: 'Product documentation and implementation guides',
        groupId: 'ecosystem',
        status: 'future',
        icon: 'book',
        hrefOverride: '/resources/docs',
      },
    ],
  },
] as const;

/** Nested paths owned by explicit App Router pages (not [slug]). */
export const technologyNestedLivePaths = [
  '/technology/platform',
  '/technology/platform/overview',
  '/technology/platform/services',
  '/technology/platform/ecosystem',
  '/technology/ai',
  '/technology/ai/principles',
  '/technology/ai/capabilities',
  '/technology/ai/human-in-the-loop',
  '/technology/ai/responsible',
  '/technology/ai/roadmap',
  '/technology/engineering',
  '/technology/engineering/architecture',
  '/technology/engineering/design-principles',
  '/technology/engineering/reliability',
  '/technology/engineering/scalability',
  '/technology/engineering/observability',
  '/technology/trust',
  '/technology/trust/privacy',
  '/technology/trust/identity',
  '/technology/trust/compliance',
  '/technology/trust/data-protection',
  '/technology/integrations',
  '/technology/integrations/apis',
  '/technology/developer',
  '/technology/roadmap',
  '/technology/why',
] as const;

/** Topics that own a dedicated `/technology/[slug]` route. */
export const technologyRoutedTopics: readonly TechnologyTopic[] = (() => {
  const bySlug = new Map<string, TechnologyTopic>();
  for (const group of technologyGroups) {
    for (const topic of group.topics) {
      if (!topic.slug) continue;
      if (topic.hrefOverride && !topic.hrefOverride.startsWith('/technology/')) {
        continue;
      }
      // Nested hubs are explicit folder routes — do not use [slug]
      if (
        topic.slug === 'platform' ||
        topic.slug === 'ai' ||
        topic.slug === 'engineering' ||
        topic.slug === 'trust' ||
        topic.slug === 'integrations' ||
        topic.slug === 'developer' ||
        topic.slug === 'roadmap'
      ) {
        continue;
      }
      if (!bySlug.has(topic.slug)) {
        bySlug.set(topic.slug, topic);
      }
    }
  }
  return [...bySlug.values()];
})();

export const technologyRouteSlugs: readonly string[] = technologyRoutedTopics.map(
  (topic) => topic.slug as string,
);

/** Slugs that render full page components instead of Coming Soon. */
export const technologyLiveSlugPages = ['architecture'] as const;

export function isTechnologyLiveSlug(
  slug: string,
): slug is (typeof technologyLiveSlugPages)[number] {
  return (technologyLiveSlugPages as readonly string[]).includes(slug);
}

export function getTechnologyTopicBySlug(slug: string): TechnologyTopic | undefined {
  return technologyRoutedTopics.find((topic) => topic.slug === slug);
}

export function getTechnologyTopicHref(topic: TechnologyTopic): string {
  if (topic.hrefOverride) return topic.hrefOverride;
  if (topic.slug) return `/technology/${topic.slug}`;
  return '/technology';
}

/** Distinct routes for sitemap (landing + nested platform + each slug). */
export function getTechnologySitemapPaths(): readonly string[] {
  const slugPaths = technologyRouteSlugs.map((slug) => `/technology/${slug}`);
  return ['/technology', ...technologyNestedLivePaths, ...slugPaths];
}

function getLiveTopicsByGroup(groupId: string): readonly TechnologyTopic[] {
  return (
    technologyGroups
      .find((g) => g.id === groupId)
      ?.topics.filter((t) => t.status === 'live') ?? []
  );
}

/** Topics shown as landing navigation cards. */
const nestedLiveGroupIds = [
  'platform',
  'intelligence',
  'engineering',
  'trust',
  'ecosystem',
] as const;

export function getTechnologyLandingCards(): readonly TechnologyTopic[] {
  const nestedLive = nestedLiveGroupIds.flatMap((id) => getLiveTopicsByGroup(id));
  const other = technologyRoutedTopics.filter(
    (t) =>
      !nestedLiveGroupIds.includes(
        t.groupId as (typeof nestedLiveGroupIds)[number],
      ) || t.slug === 'architecture',
  );
  const seen = new Set<string>();
  const cards: TechnologyTopic[] = [];
  for (const topic of [...nestedLive, ...other]) {
    const key = topic.hrefOverride ?? topic.slug ?? topic.id;
    if (seen.has(key)) continue;
    seen.add(key);
    cards.push(topic);
  }
  return cards;
}

export function getRelatedTechnologyTopics(
  slug: string,
  limit = 4,
): readonly TechnologyTopic[] {
  const current = getTechnologyTopicBySlug(slug);
  const nestedLive = nestedLiveGroupIds.flatMap((id) => getLiveTopicsByGroup(id));
  const pool = [...nestedLive, ...technologyRoutedTopics];
  if (!current) return pool.slice(0, limit);
  return pool
    .filter((topic) => topic.id !== current.id && topic.slug !== slug)
    .slice(0, limit);
}

import {
  getTechnologyTopicHref,
  technologyGroups,
} from '@/features/technology/domain';
import type { MegaMenuColumn, NavGroup, NavItem } from './types';

/**
 * Technology navigation — derived from the Technology domain registry.
 */
export const technologyGroupsNav: readonly NavGroup[] = technologyGroups.map(
  (group) => ({
    id: group.id,
    label: group.label,
    description: group.description,
    items: group.topics.map((topic) => {
      const item: NavItem = {
        id: topic.id,
        label: topic.label,
        href: getTechnologyTopicHref(topic),
        description:
          topic.status === 'future' || topic.status === 'placeholder'
            ? `${topic.description} (Coming soon)`
            : topic.description,
        ...(topic.icon ? { icon: topic.icon } : {}),
      };
      return item;
    }),
  }),
);

export const technologyMegaColumns: readonly MegaMenuColumn[] =
  technologyGroupsNav.map((group) => ({
    id: group.id,
    title: group.label,
    items: group.items,
    ...(group.id === 'ecosystem' ? { orientation: 'horizontal' as const } : {}),
  }));

/** Footer column — overview plus one topic per group. */
export const technologyFooterItems: readonly NavItem[] = [
  {
    id: 'technology-overview',
    label: 'Technology',
    href: '/technology',
    description: 'Engineering and platform foundation',
  },
  ...technologyGroups.flatMap((group) => {
    const primary =
      group.topics.find((t) => t.status === 'live') ??
      group.topics.find((t) => t.slug && t.status !== 'future') ??
      group.topics.find((t) => t.slug) ??
      group.topics[0];
    if (!primary) return [];
    return [
      {
        id: `footer-${primary.id}`,
        label: primary.label,
        href: getTechnologyTopicHref(primary),
      } satisfies NavItem,
    ];
  }),
];

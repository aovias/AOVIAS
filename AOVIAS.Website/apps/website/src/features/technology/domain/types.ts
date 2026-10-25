import type { NavIconName } from '@/config/navigation/types';

/** Topic readiness for Technology IA. */
export type TechnologyTopicStatus = 'live' | 'placeholder' | 'future';

/** Mega-menu / landing column grouping. */
export type TechnologyGroupId =
  | 'platform'
  | 'intelligence'
  | 'engineering'
  | 'trust'
  | 'ecosystem';

export type TechnologyTopic = {
  id: string;
  /** URL segment under `/technology` — omit for the landing overview. */
  slug?: string;
  label: string;
  description: string;
  groupId: TechnologyGroupId;
  status: TechnologyTopicStatus;
  icon?: NavIconName;
  /** When set, nav may point here instead of `/technology/{slug}`. */
  hrefOverride?: string;
};

export type TechnologyGroup = {
  id: TechnologyGroupId;
  label: string;
  description: string;
  topics: readonly TechnologyTopic[];
};

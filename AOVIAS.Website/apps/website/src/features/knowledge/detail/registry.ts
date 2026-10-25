import { knowledgeDetails } from '@/features/knowledge/detail/bodies';
import type { KnowledgeDetailContent } from '@/features/knowledge/detail/types';

export function getKnowledgeDetail(slug: string): KnowledgeDetailContent | undefined {
  return knowledgeDetails[slug];
}

export function getAllKnowledgeDetailSlugs(): string[] {
  return Object.keys(knowledgeDetails);
}

import {
  getPlannedIndustries,
  industryCatalog,
} from '@/features/industries/domain';
import { educationDetailContent } from '@/features/industries/detail/content/education';
import { enterpriseDetailContent } from '@/features/industries/detail/content/enterprise';
import { governmentDetailContent } from '@/features/industries/detail/content/government';
import { healthcareDetailContent } from '@/features/industries/detail/content/healthcare';
import { createFutureIndustryDetail } from '@/features/industries/detail/future-industry';
import type { IndustryDetailContent } from '@/features/industries/detail/types';

/**
 * Industry Detail Registry — maps slug → detail configuration.
 * Available industries use authored content; planned industries use sector-aware vision content.
 */
const authoredDetails: Record<string, IndustryDetailContent> = {
  healthcare: healthcareDetailContent,
  government: governmentDetailContent,
  education: educationDetailContent,
  enterprise: enterpriseDetailContent,
};

function buildRegistry(): ReadonlyMap<string, IndustryDetailContent> {
  const map = new Map<string, IndustryDetailContent>();

  for (const [slug, content] of Object.entries(authoredDetails)) {
    map.set(slug, content);
  }

  for (const industry of getPlannedIndustries()) {
    if (!map.has(industry.slug)) {
      map.set(industry.slug, createFutureIndustryDetail(industry));
    }
  }

  return map;
}

const registry = buildRegistry();

export function getIndustryDetail(slug: string): IndustryDetailContent | undefined {
  return registry.get(slug);
}

export function getAllIndustryDetailSlugs(): string[] {
  return industryCatalog.map((industry) => industry.slug);
}

export function getIndexedIndustryDetailSlugs(): string[] {
  return industryCatalog
    .filter((industry) => industry.status === 'available')
    .map((industry) => industry.slug);
}

import type { IndustryDefinition } from '@/features/industries/domain/types';
import {
  getProductById,
  type ProductDefinition,
} from '@/features/products/domain';

/**
 * AOVIAS industry catalog — single source of truth for Solutions / Industries.
 * Planned industries: add a definition; detail pages use sector-aware vision content.
 */

export const industryCatalog: readonly IndustryDefinition[] = [
  {
    id: 'healthcare',
    name: 'Healthcare',
    slug: 'healthcare',
    summary:
      'Operational clarity and disciplined delivery for care environments where continuity and accountable judgment matter.',
    description:
      'Healthcare institutions carry clinical and operational work where accuracy, handoff ownership, and uptime are inseparable from patient and public trust.',
    status: 'available',
    recommendedProductIds: ['command', 'studio', 'insight', 'flow'],
    order: 1,
    href: '/solutions/healthcare',
  },
  {
    id: 'government',
    name: 'Government',
    slug: 'government',
    summary:
      'Accountable operations and durable platforms for public programs that must withstand oversight and long lifecycles.',
    description:
      'Government programs require systems that can be explained under scrutiny, sustained across administrations, and operated without opaque tooling.',
    status: 'available',
    recommendedProductIds: ['command', 'nexus', 'sentinel', 'studio'],
    order: 2,
    href: '/solutions/government',
  },
  {
    id: 'manufacturing',
    name: 'Manufacturing',
    slug: 'manufacturing',
    summary:
      'Production continuity, quality systems, and accountable operations for complex manufacturing environments.',
    description:
      'Manufacturers depend on reliable operations across shifts and sites, governed connections among plant systems, and workflows that survive exception load.',
    status: 'available',
    recommendedProductIds: ['command', 'studio', 'platform'],
    order: 3,
    href: '/solutions/manufacturing',
  },
  {
    id: 'financial-services',
    name: 'Financial Services',
    slug: 'financial-services',
    summary:
      'Trust, risk, governance, and operational control for institutions where oversight is continuous.',
    description:
      'Financial institutions require operational clarity under oversight, evidence-friendly control trails, and platforms that keep human judgment accountable.',
    status: 'available',
    recommendedProductIds: ['command', 'studio', 'platform'],
    order: 4,
    href: '/solutions/financial-services',
  },
  {
    id: 'education',
    name: 'Education',
    slug: 'education',
    summary:
      'Coherent delivery and administrative continuity for academic institutions across academic cycles.',
    description:
      'Education institutions need technology that supports students, faculty, and staff without fragmenting standards across departments and terms.',
    status: 'available',
    recommendedProductIds: ['studio', 'insight', 'nexus', 'command'],
    order: 5,
    href: '/solutions/education',
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    slug: 'enterprise',
    summary:
      'Operational command and delivery discipline for large organizations that need governance and lasting platform value.',
    description:
      'Large enterprises need coherent systems that preserve ownership across business units, reduce operational risk, and endure beyond release cycles.',
    status: 'available',
    recommendedProductIds: ['command', 'studio', 'nexus', 'sentinel'],
    order: 6,
    href: '/solutions/enterprise',
  },
  {
    id: 'energy',
    name: 'Energy',
    slug: 'energy',
    summary:
      'Reliable operational control and institutional oversight for critical energy environments.',
    description:
      'Energy organizations operate under continuity and accountability constraints across distributed assets, partners, and exception-heavy operational work.',
    status: 'planned',
    recommendedProductIds: ['command', 'sentinel', 'flow'],
    order: 22,
    href: '/solutions/energy',
  },
  {
    id: 'transportation',
    name: 'Transportation',
    slug: 'transportation',
    summary:
      'Network coordination, continuity under disruption, and clear ownership for movement institutions.',
    description:
      'Transportation institutions need visibility across networks and partners, durable workflows under schedule pressure, and governed system connections.',
    status: 'planned',
    recommendedProductIds: ['command', 'flow', 'nexus'],
    order: 23,
    href: '/solutions/transportation',
  },
  {
    id: 'public-sector',
    name: 'Public Sector',
    slug: 'public-sector',
    summary:
      'Transparent operations and stewarded delivery for public institutions beyond core government programs.',
    description:
      'Public-sector organizations beyond core government programs need explainable operations, coherent delivery under constrained teams, and lasting assurance posture.',
    status: 'planned',
    recommendedProductIds: ['command', 'studio', 'sentinel'],
    order: 24,
    href: '/solutions/public-sector',
  },
] as const;

export function getIndustryBySlug(slug: string): IndustryDefinition | undefined {
  return industryCatalog.find((industry) => industry.slug === slug);
}

export function getAvailableIndustries(): IndustryDefinition[] {
  return industryCatalog
    .filter((industry) => industry.status === 'available')
    .sort((a, b) => a.order - b.order);
}

export function getPlannedIndustries(): IndustryDefinition[] {
  return industryCatalog
    .filter((industry) => industry.status === 'planned')
    .sort((a, b) => a.order - b.order);
}

export function getRecommendedProductsForIndustry(
  industry: IndustryDefinition,
): ProductDefinition[] {
  return industry.recommendedProductIds
    .map((id) => getProductById(id))
    .filter((product): product is ProductDefinition => Boolean(product));
}

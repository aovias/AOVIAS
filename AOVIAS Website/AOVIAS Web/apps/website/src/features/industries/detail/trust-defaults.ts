import type { IndustryDetailContent } from '@/features/industries/detail/types';

/** Shared trust framing for industry pages — industry content may override. */
export const defaultIndustryTrust: IndustryDetailContent['trust'] = {
  heading: 'Trust and compliance',
  description:
    'Security, governance, and enterprise readiness are treated as fundamentals for every industry AOVIAS serves.',
  signals: [
    {
      id: 'security',
      label: 'Security-minded',
      description: 'Controls as product fundamentals',
      variant: 'security',
    },
    {
      id: 'governance',
      label: 'Governance',
      description: 'Clear ownership and accountability',
      variant: 'compliance',
    },
    {
      id: 'enterprise',
      label: 'Enterprise ready',
      description: 'Built for institutional constraints',
      variant: 'quality',
    },
  ],
  highlights: [
    'Measured claims without artificial urgency',
    'Access and actions designed for auditability',
    'Long-term stewardship over short release cycles',
  ],
  controls: [
    {
      id: 'access',
      title: 'Access discipline',
      description: 'Identity-aware access aligned to institutional roles.',
      category: 'Security',
    },
    {
      id: 'accountability',
      title: 'Accountable operations',
      description: 'Ownership remains visible across workflows and handoffs.',
      category: 'Governance',
    },
    {
      id: 'continuity',
      title: 'Operational continuity',
      description: 'Patterns that favor reliability under institutional pressure.',
      category: 'Reliability',
    },
  ],
  frameworks: [
    {
      id: 'oversight',
      name: 'Oversight alignment',
      description: 'Designed to support institutional evaluation and review.',
    },
    {
      id: 'boundaries',
      name: 'Data boundaries',
      description: 'Institution-scoped environments and clear ownership.',
    },
    {
      id: 'assurance',
      name: 'Assurance readiness',
      description: 'Evidence-friendly design for compliance conversations.',
    },
  ],
};

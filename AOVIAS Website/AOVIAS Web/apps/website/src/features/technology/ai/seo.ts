import type { BreadcrumbItem } from '@/lib/navigation/breadcrumb-types';

const base: BreadcrumbItem[] = [
  { label: 'Home', href: '/' },
  { label: 'Technology', href: '/technology' },
];

export const aiPagesSeo = {
  hub: {
    title: 'AI at AOVIAS',
    description:
      'How AOVIAS applies Artificial Intelligence as a platform capability — assisting people, preserving accountability, and reinforcing institutional trust.',
    path: '/technology/ai',
    keywords: [
      'enterprise AI',
      'responsible AI',
      'AOVIAS',
      'human-in-the-loop',
      'AI-assisted operations',
    ],
  },
  principles: {
    title: 'AI Principles',
    description:
      'Enduring AI principles: human-centered design, transparency, explainability, accountability, privacy, security, fairness, reliability, and continuous improvement.',
    path: '/technology/ai/principles',
    keywords: ['AI principles', 'responsible AI', 'explainability', 'AOVIAS'],
  },
  capabilities: {
    title: 'AI Capabilities',
    description:
      'Outcome-oriented AI capabilities for recommendations, workflow assistance, knowledge discovery, compliance guidance, insights, and natural language assistance — without autonomous decision-making claims.',
    path: '/technology/ai/capabilities',
    keywords: ['AI capabilities', 'enterprise AI', 'decision support', 'AOVIAS'],
  },
  hitl: {
    title: 'Human-in-the-Loop',
    description:
      'AI proposes; humans review, approve, and remain accountable. Institutional responsibility always stays with people.',
    path: '/technology/ai/human-in-the-loop',
    keywords: [
      'human-in-the-loop',
      'AI governance',
      'human accountability',
      'enterprise AI',
    ],
  },
  responsible: {
    title: 'Responsible AI',
    description:
      'Responsible AI by design: privacy, secure processing, auditability, traceability, bias awareness, governance, and enterprise controls — principles, not unsupported certifications.',
    path: '/technology/ai/responsible',
    keywords: [
      'responsible AI',
      'AI governance',
      'enterprise AI',
      'AI auditability',
      'AOVIAS',
    ],
  },
  roadmap: {
    title: 'AI Roadmap',
    description:
      'High-level AI capability themes — assist, recommend, understand, coordinate, automate repetitive tasks, enable better decisions — without speculative commitments.',
    path: '/technology/ai/roadmap',
    keywords: ['AI roadmap', 'enterprise AI', 'AOVIAS platform'],
  },
} as const;

export function aiHubBreadcrumbs(): BreadcrumbItem[] {
  return [...base, { label: 'AI', href: '/technology/ai', current: true }];
}

export function aiPrinciplesBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'AI', href: '/technology/ai' },
    { label: 'Principles', href: '/technology/ai/principles', current: true },
  ];
}

export function aiCapabilitiesBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'AI', href: '/technology/ai' },
    { label: 'Capabilities', href: '/technology/ai/capabilities', current: true },
  ];
}

export function aiHitlBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'AI', href: '/technology/ai' },
    {
      label: 'Human-in-the-Loop',
      href: '/technology/ai/human-in-the-loop',
      current: true,
    },
  ];
}

export function aiResponsibleBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'AI', href: '/technology/ai' },
    { label: 'Responsible AI', href: '/technology/ai/responsible', current: true },
  ];
}

export function aiRoadmapBreadcrumbs(): BreadcrumbItem[] {
  return [
    ...base,
    { label: 'AI', href: '/technology/ai' },
    { label: 'Roadmap', href: '/technology/ai/roadmap', current: true },
  ];
}

export const aiSitemapPaths = [
  '/technology/ai',
  '/technology/ai/principles',
  '/technology/ai/capabilities',
  '/technology/ai/human-in-the-loop',
  '/technology/ai/responsible',
  '/technology/ai/roadmap',
] as const;

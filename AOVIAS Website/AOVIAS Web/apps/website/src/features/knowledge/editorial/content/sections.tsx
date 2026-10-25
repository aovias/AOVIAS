import type { KnowledgeEditorialContent } from '@/features/knowledge/editorial/types';

/** Shared library category strip for knowledge hubs. */
export const knowledgeLibraryCategories = [
  {
    id: 'platform',
    name: 'Platform',
    href: '/resources/categories/platform',
    description: 'Shared foundation and architecture',
  },
  {
    id: 'ai',
    name: 'AI',
    href: '/resources/categories/governance',
    description: 'Responsible intelligence',
  },
  {
    id: 'engineering',
    name: 'Engineering',
    href: '/resources/categories/engineering',
  },
  {
    id: 'security',
    name: 'Security',
    href: '/resources/categories/governance',
  },
  {
    id: 'governance',
    name: 'Governance',
    href: '/resources/categories/governance',
  },
  {
    id: 'healthcare',
    name: 'Healthcare',
    href: '/solutions/healthcare',
  },
  {
    id: 'government',
    name: 'Government',
    href: '/solutions/government',
  },
  {
    id: 'manufacturing',
    name: 'Manufacturing',
    href: '/solutions/manufacturing',
  },
  {
    id: 'financial',
    name: 'Financial Services',
    href: '/solutions/financial-services',
  },
  {
    id: 'guides',
    name: 'Product Guides',
    href: '/resources/categories/guides',
  },
] as const;

export const blogEditorialContent = {
  id: 'blog',
  atmosphere: 'aurora',
  cover: {
    label: 'Blog — thought leadership',
    tone: 'primary',
    motif: 'architecture',
  },
  kinds: ['article'],
  hero: {
    eyebrow: 'Blog',
    title: 'Thought leadership for institutional technology.',
    description:
      'Engineering culture, industry commentary, and disciplined product writing — education first, promotion never.',
    primaryCta: { label: 'Search knowledge', href: '/resources/search' },
    secondaryCta: { label: 'Browse categories', href: '/resources/categories' },
  },
  orient: {
    title: 'A publication, not a news feed.',
    description:
      'AOVIAS Blog exists to teach how institutional software is designed, released, and stewarded. Articles reward careful reading.',
  },
  libraryNav: {
    title: 'Explore the knowledge library',
    description:
      'Move by topic — not endless tags. Categories connect blog writing to the wider AOVIAS Knowledge platform.',
    categories: knowledgeLibraryCategories,
  },
  featured: {
    title: 'Featured article',
    description: 'Flagship editorial from AOVIAS Engineering.',
  },
  grid: {
    title: 'Editorial collection',
    description: 'Magazine layout for flagship and supporting articles.',
  },
  learningPath: {
    title: 'Understanding AOVIAS through writing',
    description:
      'A guided path from institutional trust to release discipline and operational excellence.',
    steps: [
      'Understanding AOVIAS',
      'Platform fundamentals',
      'Operations',
      'Governance',
      'Deployment',
      'Enterprise scale',
    ],
  },
  closing: {
    title: 'Continue learning',
    description:
      'Deepen with documentation, or request a demonstration of the products these articles describe.',
    primaryCta: { label: 'Explore documentation', href: '/resources/docs' },
    secondaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
  },
} as const satisfies KnowledgeEditorialContent;

export const insightsEditorialContent = {
  id: 'insights',
  atmosphere: 'trust-geometry',
  cover: {
    label: 'Insights — executive review',
    tone: 'info',
    motif: 'ai',
  },
  kinds: ['insight'],
  hero: {
    eyebrow: 'Insights',
    title: 'Research and architecture thinking for leaders.',
    description:
      'Operational excellence, industry observation, and executive perspectives — closer to a consulting publication than a blog.',
    primaryCta: { label: 'Search insights', href: '/resources/search?kind=insight' },
    secondaryCta: { label: 'Browse categories', href: '/resources/categories' },
  },
  orient: {
    title: 'Long-term thinking, measured claims.',
    description:
      'Insights help evaluators and operators reason about governance, AI, and clarity under real institutional constraints.',
  },
  libraryNav: {
    title: 'Topics that shape insight',
    description: 'Connect research to platform, industries, and governance practice.',
    categories: knowledgeLibraryCategories,
  },
  featured: {
    title: 'Featured insight',
    description: 'A flagship note for technical and executive readers.',
  },
  grid: {
    title: 'Insight collection',
    description: 'Editorial rhythm for research and observation.',
  },
  learningPath: {
    title: 'From observation to institutional judgment',
    description: 'A path through clarity, responsibility, and accountable operations.',
    steps: [
      'Observe',
      'Frame',
      'Govern',
      'Operate',
      'Review',
      'Improve',
    ],
  },
  closing: {
    title: 'Continue learning',
    description:
      'Pair insights with documentation, or speak with AOVIAS about your evaluation context.',
    primaryCta: { label: 'Explore documentation', href: '/resources/docs' },
    secondaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
  },
} as const satisfies KnowledgeEditorialContent;

export const docsEditorialContent = {
  id: 'docs',
  atmosphere: 'blueprint-grid',
  cover: {
    label: 'Documentation — knowledge system',
    tone: 'secondary',
    motif: 'architecture',
  },
  kinds: ['documentation'],
  hero: {
    eyebrow: 'Documentation',
    title: 'Structured product documentation.',
    description:
      'Learning, configuration, and architecture guidance for Command, Studio, and Platform — first-class readability for builders and evaluators.',
    primaryCta: { label: 'Search docs', href: '/resources/search?kind=documentation' },
    secondaryCta: { label: 'Platform overview', href: '/resources/docs/platform-overview' },
  },
  orient: {
    title: 'Documentation as product.',
    description:
      'AOVIAS treats docs as a learning system: clear navigation, durable orientation, and architecture that can grow into API reference without redesign.',
  },
  libraryNav: {
    title: 'Find documentation by topic',
    description: 'Library categories that connect docs to the wider knowledge platform.',
    categories: knowledgeLibraryCategories,
  },
  featured: {
    title: 'Featured guide',
    description: 'Start here for platform orientation.',
  },
  grid: {
    title: 'Documentation library',
    description: 'Guides organized for progressive understanding.',
  },
  learningPath: {
    title: 'Documentation learning path',
    description: 'From platform orientation to product getting-started and delivery practice.',
    steps: [
      'Platform overview',
      'Command basics',
      'Studio delivery',
      'Governance',
      'Operations',
      'Enterprise scale',
    ],
  },
  closing: {
    title: 'Continue learning',
    description:
      'Browse the resource library, or request a demonstration alongside documentation review.',
    primaryCta: { label: 'Browse resources', href: '/resources/library' },
    secondaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
  },
} as const satisfies KnowledgeEditorialContent;

export const libraryEditorialContent = {
  id: 'library',
  atmosphere: 'knowledge-pattern',
  cover: {
    label: 'Resources — reference library',
    tone: 'success',
    motif: 'operations',
  },
  kinds: ['resource', 'release-note'],
  hero: {
    eyebrow: 'Resources',
    title: 'A professional reference library.',
    description:
      'Guides, checklists, whitepapers-ready material, and release notes — organized for diligence, not distraction.',
    primaryCta: { label: 'Search resources', href: '/resources/search' },
    secondaryCta: { label: 'Browse categories', href: '/resources/categories' },
  },
  orient: {
    title: 'Downloads and reference with purpose.',
    description:
      'Resources support evaluation and adoption: structured artifacts teams can use in diligence conversations and implementation planning.',
  },
  libraryNav: {
    title: 'Library categories',
    description: 'Navigate the knowledge ecosystem by institutional topic.',
    categories: knowledgeLibraryCategories,
  },
  featured: {
    title: 'Featured resource',
    description: 'Practical material for evaluation and stewardship.',
  },
  grid: {
    title: 'Reference collection',
    description: 'Guides and notes arranged for professional use.',
  },
  learningPath: {
    title: 'From evaluation to adoption',
    description: 'A path through diligence materials and platform orientation.',
    steps: [
      'Evaluate',
      'Orient',
      'Govern',
      'Adopt',
      'Operate',
      'Improve',
    ],
  },
  closing: {
    title: 'Continue learning',
    description:
      'Return to documentation for implementation depth, or request a demo with your evaluation checklist in hand.',
    primaryCta: { label: 'Explore documentation', href: '/resources/docs' },
    secondaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
  },
} as const satisfies KnowledgeEditorialContent;

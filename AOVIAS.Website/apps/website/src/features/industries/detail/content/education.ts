import { getIndustryBySlug } from '@/features/industries/domain';
import { standardIndustryNav } from '@/features/industries/detail/shared';
import { defaultIndustryTrust } from '@/features/industries/detail/trust-defaults';
import type { IndustryDetailContent } from '@/features/industries/detail/types';

const industry = getIndustryBySlug('education');

export const educationDetailContent = {
  slug: 'education',
  name: industry?.name ?? 'Education',
  status: 'available',
  seo: {
    title: 'Education',
    description:
      'AOVIAS for education — coherent delivery and operational clarity for academic and administrative institutions across academic cycles.',
    keywords: [
      'AOVIAS education',
      'higher education platforms',
      'academic administration technology',
      'education institutional software',
    ],
  },
  navigation: standardIndustryNav,
  hero: {
    eyebrow: 'Education',
    headline: 'Platforms that respect academic and administrative stewardship',
    challengesSummary:
      'Schools and universities must serve students, faculty, and staff across academic cycles — without letting local tools erode shared standards.',
    primaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
    secondaryCta: { label: 'Explore products', href: '/products' },
    illustrationLabel: 'Education — learning and administration',
  },
  overview: {
    heading: 'Institutional context',
    description:
      'Education institutions steward people, knowledge, and public trust. Academic calendars create recurring peaks; administrative teams keep services running while faculty and researchers pursue missions that do not pause for IT churn. Technology should strengthen that stewardship, not add another layer of fragmentation.',
    opportunities:
      'Institutions that standardize how solutions are designed and how operational work is owned can modernize student services, research support, and campus operations without resetting standards each academic year. AOVIAS Studio leads for coherent delivery; AOVIAS Command supports administrative continuity. Insight and interoperability products extend decision support as data and systems mature.',
  },
  challenges: {
    heading: 'Education challenges',
    description:
      'Pressures familiar to academic and administrative leadership.',
    items: [
      {
        id: 'academic-ops',
        title: 'Disconnected academic and campus operations',
        description:
          'Teaching, research support, student services, and facilities often run on separate tools, making ownership and status hard to reconcile.',
      },
      {
        id: 'capacity',
        title: 'Modernization under fixed capacity',
        description:
          'IT and administrative teams must improve systems while keeping day-to-day service intact through enrollment peaks and term transitions.',
      },
      {
        id: 'local-tools',
        title: 'Local solutions that erode standards',
        description:
          'Departments adopt point tools that solve immediate needs but make quality, access, and interoperability harder for the institution over time.',
      },
      {
        id: 'knowledge',
        title: 'Knowledge lost between cycles',
        description:
          'Staffing changes and academic-year handoffs drop context when systems fail to preserve how work was owned and completed.',
      },
    ],
  },
  solutions: {
    heading: 'How AOVIAS helps',
    description:
      'AOVIAS focuses on coherent institutional delivery, operational clarity, and responsible decision support for education.',
    items: [
      {
        id: 'studio',
        heading: 'AOVIAS Studio for institutional delivery',
        description:
          'Studio helps academic technology and administrative teams design and ship solutions with shared standards that outlast a single term or project.',
      },
      {
        id: 'command',
        heading: 'AOVIAS Command for administrative continuity',
        description:
          'Command supports structured visibility where campus and administrative operations need clear ownership across teams and cycles.',
      },
      {
        id: 'insight',
        heading: 'Responsible insight and connection',
        description:
          'Insight and Nexus are framed for decision support and governed connectivity — keeping human judgment accountable as institutional data grows.',
      },
    ],
  },
  products: {
    heading: 'Recommended products',
    description:
      'Products that strengthen delivery standards, administrative operations, insight, and interoperable institutional systems.',
    productIds: industry?.recommendedProductIds ?? ['studio', 'command'],
  },
  benefits: {
    heading: 'Organizational outcomes',
    description:
      'Outcomes education institutions can evaluate across academic and administrative work.',
    items: [
      {
        id: 'standards',
        heading: 'Stronger delivery standards',
        description:
          'Fewer one-off departmental builds that quietly diverge from institutional practice.',
      },
      {
        id: 'continuity',
        heading: 'Continuity across academic cycles',
        description:
          'Operational context survives term transitions, staffing change, and recurring peak periods.',
      },
      {
        id: 'governance',
        heading: 'Clearer initiative ownership',
        description:
          'Leadership can see who owns campus and academic technology work and how it progresses.',
      },
      {
        id: 'service',
        heading: 'More consistent campus service',
        description:
          'Students, faculty, and staff encounter fewer gaps caused by fragmented tools and unclear handoffs.',
      },
      {
        id: 'stewardship',
        heading: 'Knowledge stewardship',
        description:
          'Institutional context stays attached to work instead of living only in individual inboxes.',
      },
    ],
  },
  trust: {
    ...defaultIndustryTrust,
    description:
      'Education institutions deserve security, clarity, and technology that respects long-lived academic missions — with measured claims only.',
  },
  resources: {
    heading: 'Resources',
    description: 'Guides and documentation for education evaluation.',
    items: [
      {
        id: 'edu-docs',
        kind: 'documentation',
        title: 'Studio delivery',
        description:
          'How AOVIAS Studio supports disciplined delivery for institutional work.',
        href: '/resources/docs/studio-delivery',
        category: { id: 'docs', name: 'Documentation', tone: 'primary' },
      },
      {
        id: 'edu-guide',
        kind: 'article',
        title: 'Release discipline',
        description:
          'Practices for consistent, accountable delivery across long-lived institutional cycles.',
        href: '/resources/blog/release-discipline',
        category: { id: 'blog', name: 'Articles', tone: 'info' },
      },
      {
        id: 'edu-eval',
        kind: 'resource',
        title: 'Evaluation checklist',
        description:
          'A practical checklist for institutions assessing AOVIAS platform fit.',
        href: '/resources/library/evaluation-checklist',
        category: { id: 'library', name: 'Library', tone: 'secondary' },
      },
    ],
  },
  primaryCtaBand: {
    title: 'Explore education solutions with AOVIAS',
    description:
      'Request a demonstration to evaluate fit for your institution, or review the products most relevant to education.',
    primaryCta: { label: 'Request a demo', href: '/company/contact#form-demo' },
    secondaryCta: { label: 'Explore products', href: '/products' },
  },
} as const satisfies IndustryDetailContent;

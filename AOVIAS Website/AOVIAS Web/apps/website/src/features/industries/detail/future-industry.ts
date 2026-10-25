import type { IndustryDefinition } from '@/features/industries/domain';
import { standardIndustryNav } from '@/features/industries/detail/shared';
import { defaultIndustryTrust } from '@/features/industries/detail/trust-defaults';
import type {
  IndustryDetailContent,
  IndustrySolutionPoint,
} from '@/features/industries/detail/types';
import type {
  IndustryBenefit,
  IndustryChallenge,
} from '@/features/industries/domain';

type SectorFraming = {
  overviewHeading: string;
  opportunities: string;
  challengesHeading: string;
  challengesDescription: string;
  challenges: readonly Omit<IndustryChallenge, 'id'>[];
  solutionsHeading: string;
  solutionsDescription: string;
  solutions: readonly Omit<IndustrySolutionPoint, 'id'>[];
  productsDescription: string;
  benefitsHeading: string;
  benefits: readonly Omit<IndustryBenefit, 'id'>[];
  trustDescription: string;
  ctaTitle: string;
  ctaDescription: string;
};

const defaultFraming = (industry: IndustryDefinition): SectorFraming => ({
  overviewHeading: `Direction for ${industry.name}`,
  opportunities:
    'AOVIAS expands sector coverage through the same platform principles — operational clarity, disciplined delivery, and accountable stewardship — adapted to how this sector works.',
  challengesHeading: 'Anticipated challenges',
  challengesDescription:
    'Representative pressures for this sector — presented as direction, not a finished research claim.',
  challenges: [
    {
      title: 'Operational complexity',
      description:
        'Critical work spans teams and systems where ownership and continuity are hard to maintain.',
    },
    {
      title: 'Governance pressure',
      description:
        'Oversight expectations grow while tools often fail to preserve clear accountability.',
    },
    {
      title: 'Reliability risk',
      description:
        'Ambiguity and brittle processes create cost where institutions cannot afford surprise.',
    },
    {
      title: 'Change without stewardship',
      description:
        'Capability arrives faster than institutions can standardize, train, and sustain it.',
    },
  ],
  solutionsHeading: 'How AOVIAS intends to help',
  solutionsDescription:
    'Future industry coverage will use AOVIAS platform principles: clarity, accountability, and long-term operability.',
  solutions: [
    {
      heading: 'Operational clarity',
      description: 'Structured visibility for work that carries institutional responsibility.',
    },
    {
      heading: 'Disciplined delivery',
      description: 'Shared standards so solutions remain coherent across teams.',
    },
    {
      heading: 'Trust by design',
      description: 'Security and governance treated as fundamentals, not add-ons.',
    },
  ],
  productsDescription:
    'Recommended AOVIAS products based on sector needs — availability varies by product status.',
  benefitsHeading: 'Intended outcomes',
  benefits: [
    {
      heading: 'Better governance',
      description: 'Clearer ownership across operational and delivery work.',
    },
    {
      heading: 'Operational efficiency',
      description: 'Less fragmentation across tools and unclear handoffs.',
    },
    {
      heading: 'Compliance readiness',
      description: 'Systems shaped for oversight and evidence-friendly review.',
    },
    {
      heading: 'Transparency',
      description: 'Work that leadership can understand and steward.',
    },
  ],
  trustDescription:
    'Even in vision form, industry pages state status clearly and avoid availability claims.',
  ctaTitle: 'Stay oriented to current industries',
  ctaDescription:
    'Explore sectors AOVIAS serves today, or speak with us about long-term institutional needs.',
});

const sectorFramingBySlug: Record<string, (industry: IndustryDefinition) => SectorFraming> = {
  manufacturing: (industry) => ({
    overviewHeading: 'Direction for manufacturing operations',
    opportunities:
      'Manufacturers that depend on production continuity can benefit from clearer operational control, governed shop-floor and plant data connections, and workflow discipline — without treating modernization as a slogan. This page describes long-term market direction, not a committed offering or schedule.',
    challengesHeading: 'Manufacturing pressures',
    challengesDescription:
      'Representative pressures in production environments — framed as direction, not finished research.',
    challenges: [
      {
        title: 'Production visibility across shifts and sites',
        description:
          'Critical work moves across lines, plants, and partners where status and ownership are hard to keep aligned.',
      },
      {
        title: 'Quality and change under throughput pressure',
        description:
          'Process changes and exceptions accumulate faster than teams can document ownership and evidence.',
      },
      {
        title: 'Brittle integrations around the plant',
        description:
          'Operational systems connect unevenly, creating silent gaps when production demand peaks.',
      },
      {
        title: 'Knowledge loss across workforce transitions',
        description:
          'Experienced operators leave; undocumented procedures and tool sprawl make continuity fragile.',
      },
    ],
    solutionsHeading: 'How AOVIAS intends to help',
    solutionsDescription:
      'Manufacturing coverage would emphasize AOVIAS Command for operational control, Flow for disciplined workflow, and Nexus for governed connectivity — availability varies by product status.',
    solutions: [
      {
        heading: 'Operational control on the plant floor and beyond',
        description:
          'Command is positioned for structured visibility where production continuity depends on attributable action.',
      },
      {
        heading: 'Workflow discipline under real constraints',
        description:
          'Flow is framed for institutional workflows that must remain operable across shifts and exceptions.',
      },
      {
        heading: 'Governed connections across systems',
        description:
          'Nexus is positioned to reduce uncontrolled integration sprawl around manufacturing operations.',
      },
    ],
    productsDescription: `Likely product fit for ${industry.name}: Command, Flow, and Nexus — status varies; this is direction, not availability.`,
    benefitsHeading: 'Intended outcomes',
    benefits: [
      {
        heading: 'Clearer production ownership',
        description: 'Escalations and exceptions carry named responsibility across sites and shifts.',
      },
      {
        heading: 'Fewer silent integration gaps',
        description: 'Connections between operational systems are easier to govern and explain.',
      },
      {
        heading: 'More durable process change',
        description: 'Workflow improvements persist beyond a single project or plant initiative.',
      },
      {
        heading: 'Continuity under workforce change',
        description: 'Operational context is less dependent on individual memory alone.',
      },
    ],
    trustDescription:
      'Manufacturing vision pages state direction clearly and avoid inventing certifications, customers, or schedules.',
    ctaTitle: 'Explore industries AOVIAS serves today',
    ctaDescription:
      'Review current sector pages, or talk with AOVIAS about long-term manufacturing institutional needs.',
  }),

  'financial-services': (industry) => ({
    overviewHeading: 'Direction for financial institutions',
    opportunities:
      'Financial institutions that require control, auditability, and responsible decision support can align with AOVIAS platform principles — operational command, assurance, and accountable insight. This page describes long-term market direction, not a committed offering or schedule.',
    challengesHeading: 'Financial services pressures',
    challengesDescription:
      'Representative pressures where trust and control are foundational — framed as direction, not finished research.',
    challenges: [
      {
        title: 'Control and evidence under oversight',
        description:
          'Operational and risk conversations require clear ownership trails — not reconstructed after the fact.',
      },
      {
        title: 'Fragmented operational tooling',
        description:
          'Lines of business adopt disconnected systems that obscure enterprise accountability and raise review cost.',
      },
      {
        title: 'Decision support without opaque automation',
        description:
          'Institutions need insight that keeps human judgment accountable rather than hiding how conclusions were reached.',
      },
      {
        title: 'Change paced by governance',
        description:
          'Capability must advance without outrunning access control, model oversight, and operational sustainment.',
      },
    ],
    solutionsHeading: 'How AOVIAS intends to help',
    solutionsDescription:
      'Financial services coverage would emphasize AOVIAS Command for operational control, Sentinel for assurance orientation, and Insight for accountable decision support — availability varies by product status.',
    solutions: [
      {
        heading: 'Operational command under scrutiny',
        description:
          'Command is positioned for structured visibility where institutional work must remain attributable.',
      },
      {
        heading: 'Assurance as a lasting capability',
        description:
          'Sentinel frames vigilance and oversight as long-term institutional posture, not a one-time checklist.',
      },
      {
        heading: 'Insight with accountable judgment',
        description:
          'Insight is framed as decision support that keeps human oversight visible and purposeful.',
      },
    ],
    productsDescription: `Likely product fit for ${industry.name}: Command, Sentinel, and Insight — status varies; this is direction, not availability.`,
    benefitsHeading: 'Intended outcomes',
    benefits: [
      {
        heading: 'Stronger control narratives',
        description: 'Ownership and status are easier to explain in risk and leadership review.',
      },
      {
        heading: 'Less operational fragmentation',
        description: 'Fewer disconnected tools obscuring who owns critical institutional work.',
      },
      {
        heading: 'Responsible decision support',
        description: 'Insight products reinforce judgment rather than replacing accountability.',
      },
      {
        heading: 'Governed capability growth',
        description: 'New capability can be introduced with clearer oversight and sustainment paths.',
      },
    ],
    trustDescription:
      'Financial services vision pages avoid inventing certifications, customer names, or availability dates.',
    ctaTitle: 'Explore industries AOVIAS serves today',
    ctaDescription:
      'Review current sector pages, or talk with AOVIAS about long-term needs in financial institutions.',
  }),

  energy: (industry) => ({
    overviewHeading: 'Direction for energy operations',
    opportunities:
      'Energy organizations that operate under continuity and accountability constraints can align with AOVIAS principles — reliable operational control, assurance, and disciplined workflow. This page describes long-term market direction, not a committed offering or schedule.',
    challengesHeading: 'Energy sector pressures',
    challengesDescription:
      'Representative pressures in critical energy operations — framed as direction, not finished research.',
    challenges: [
      {
        title: 'Continuity for critical operations',
        description:
          'Infrastructure and operational teams cannot absorb surprise when status and ownership are unclear.',
      },
      {
        title: 'Oversight across distributed assets',
        description:
          'Work spans sites, partners, and control environments where accountability is easy to lose.',
      },
      {
        title: 'Incident and exception load',
        description:
          'Exceptions accumulate; brittle process tooling raises risk precisely when reliability matters most.',
      },
      {
        title: 'Change under safety and stewardship duty',
        description:
          'New systems must respect institutional duty — capability without clear ownership is not progress.',
      },
    ],
    solutionsHeading: 'How AOVIAS intends to help',
    solutionsDescription:
      'Energy coverage would emphasize AOVIAS Command for operational control, Sentinel for assurance, and Flow for durable workflows — availability varies by product status.',
    solutions: [
      {
        heading: 'Command for critical operational work',
        description:
          'Command is positioned for structured visibility where energy operations require attributable control.',
      },
      {
        heading: 'Assurance for institutional vigilance',
        description:
          'Sentinel frames oversight as a lasting capability suited to environments under continuous accountability.',
      },
      {
        heading: 'Workflows that survive exception load',
        description:
          'Flow is framed for institutional processes that must remain operable when conditions change.',
      },
    ],
    productsDescription: `Likely product fit for ${industry.name}: Command, Sentinel, and Flow — status varies; this is direction, not availability.`,
    benefitsHeading: 'Intended outcomes',
    benefits: [
      {
        heading: 'Clearer operational accountability',
        description: 'Critical work carries named ownership across sites and teams.',
      },
      {
        heading: 'Stronger oversight posture',
        description: 'Leadership can see how vigilance and escalation are structured.',
      },
      {
        heading: 'More durable exception handling',
        description: 'Workflows remain coherent when demand and incidents spike.',
      },
      {
        heading: 'Stewarded change',
        description: 'Capability growth respects continuity and institutional duty.',
      },
    ],
    trustDescription:
      'Energy vision pages state direction clearly and do not invent certifications, customers, or schedules.',
    ctaTitle: 'Explore industries AOVIAS serves today',
    ctaDescription:
      'Review current sector pages, or talk with AOVIAS about long-term energy institutional needs.',
  }),

  transportation: (industry) => ({
    overviewHeading: 'Direction for transportation networks',
    opportunities:
      'Transportation institutions that keep people and goods moving need visibility, continuity, and clear ownership across networks and partners. AOVIAS platform principles — Command, Flow, and Nexus — align with that direction. This page is not a committed offering or schedule.',
    challengesHeading: 'Transportation pressures',
    challengesDescription:
      'Representative pressures in network operations — framed as direction, not finished research.',
    challenges: [
      {
        title: 'Coordination across networks and partners',
        description:
          'Status and ownership fragment when operators, agencies, and partners work from disconnected views.',
      },
      {
        title: 'Continuity under schedule pressure',
        description:
          'Disruptions and handoffs create risk when escalation paths and operational context are unclear.',
      },
      {
        title: 'Integration sprawl along the corridor',
        description:
          'Systems accumulate without governed connection patterns, raising cost and silent failure modes.',
      },
      {
        title: 'Workforce and shift transitions',
        description:
          'Knowledge drops between shifts and contractors when tools fail to preserve how work was owned.',
      },
    ],
    solutionsHeading: 'How AOVIAS intends to help',
    solutionsDescription:
      'Transportation coverage would emphasize AOVIAS Command for operational coordination, Flow for durable workflows, and Nexus for governed connectivity — availability varies by product status.',
    solutions: [
      {
        heading: 'Command for network operations',
        description:
          'Command is positioned for structured visibility where movement networks depend on clear ownership.',
      },
      {
        heading: 'Flow under real-world disruption',
        description:
          'Flow is framed for institutional workflows that must remain operable when schedules and exceptions shift.',
      },
      {
        heading: 'Governed network connectivity',
        description:
          'Nexus is positioned to connect institutional systems without uncontrolled sprawl across partners.',
      },
    ],
    productsDescription: `Likely product fit for ${industry.name}: Command, Flow, and Nexus — status varies; this is direction, not availability.`,
    benefitsHeading: 'Intended outcomes',
    benefits: [
      {
        heading: 'Clearer network ownership',
        description: 'Handoffs and escalations carry responsibility across operators and partners.',
      },
      {
        heading: 'Better continuity under disruption',
        description: 'Operational context survives shift changes and exception spikes.',
      },
      {
        heading: 'Less integration chaos',
        description: 'Connections between systems are easier to govern and explain.',
      },
      {
        heading: 'Coherent workflow practice',
        description: 'Process improvements endure beyond a single corridor or project.',
      },
    ],
    trustDescription:
      'Transportation vision pages avoid inventing certifications, customer names, or availability dates.',
    ctaTitle: 'Explore industries AOVIAS serves today',
    ctaDescription:
      'Review current sector pages, or talk with AOVIAS about long-term transportation institutional needs.',
  }),

  'public-sector': (industry) => ({
    overviewHeading: 'Direction for broader public-sector institutions',
    opportunities:
      'Public-sector organizations beyond core government programs — including civic services and publicly accountable institutions — need transparent, stewarded systems. AOVIAS Command, Studio, and Sentinel align with that direction. This page is not a committed offering or schedule.',
    challengesHeading: 'Public-sector pressures',
    challengesDescription:
      'Representative pressures for publicly accountable institutions — framed as direction, not finished research.',
    challenges: [
      {
        title: 'Transparency without slowing service',
        description:
          'Institutions must explain decisions and access while still delivering day-to-day public services.',
      },
      {
        title: 'Delivery across constrained teams',
        description:
          'Smaller technology teams must modernize without fragmenting standards or losing operational ownership.',
      },
      {
        title: 'Assurance expectations that grow',
        description:
          'Oversight and evidence demands increase while tools often obscure how work actually moves.',
      },
      {
        title: 'Continuity across funding and staffing cycles',
        description:
          'Programs and services must survive contractor change and budget cycles without quiet degradation.',
      },
    ],
    solutionsHeading: 'How AOVIAS intends to help',
    solutionsDescription:
      'Public-sector coverage would emphasize AOVIAS Command for accountable operations, Studio for coherent delivery, and Sentinel for assurance orientation — availability varies by product status.',
    solutions: [
      {
        heading: 'Command for accountable public operations',
        description:
          'Command is positioned for structured visibility where public services depend on reliable ownership.',
      },
      {
        heading: 'Studio for coherent institutional delivery',
        description:
          'Studio helps constrained teams design and ship solutions with shared standards that endure.',
      },
      {
        heading: 'Sentinel for lasting assurance posture',
        description:
          'Sentinel frames vigilance as an institutional capability suited to publicly accountable environments.',
      },
    ],
    productsDescription: `Likely product fit for ${industry.name}: Command, Studio, and Sentinel — status varies; this is direction, not availability.`,
    benefitsHeading: 'Intended outcomes',
    benefits: [
      {
        heading: 'Explainable public operations',
        description: 'Work and ownership can be described clearly under review.',
      },
      {
        heading: 'Delivery that stays coherent',
        description: 'Solutions share standards instead of proliferating one-off tools.',
      },
      {
        heading: 'Stronger assurance conversations',
        description: 'Oversight is supported by design, not after-the-fact reconstruction.',
      },
      {
        heading: 'Continuity across cycles',
        description: 'Services remain operable through staffing and funding transitions.',
      },
    ],
    trustDescription:
      'Public-sector vision pages state status clearly and avoid inventing certifications, customers, or schedules.',
    ctaTitle: 'Explore industries AOVIAS serves today',
    ctaDescription:
      'Review current sector pages, or talk with AOVIAS about long-term public-sector institutional needs.',
  }),
};

function resolveSectorFraming(industry: IndustryDefinition): SectorFraming {
  const builder = sectorFramingBySlug[industry.slug];
  return builder ? builder(industry) : defaultFraming(industry);
}

/**
 * Builds Industry Detail content for planned (vision) industries from catalog config,
 * with sector-specific framing where defined.
 */
export function createFutureIndustryDetail(
  industry: IndustryDefinition,
): IndustryDetailContent {
  const framing = resolveSectorFraming(industry);

  return {
    slug: industry.slug,
    name: industry.name,
    status: industry.status,
    seo: {
      title: industry.name,
      description: `${industry.description} This page describes long-term market direction — not a committed service offering or timeline.`,
      keywords: [
        industry.name,
        'AOVIAS',
        'institutional technology',
        'industry solutions',
      ],
    },
    navigation: standardIndustryNav,
    hero: {
      eyebrow: 'Vision industry',
      headline: industry.summary,
      challengesSummary: industry.description,
      primaryCta: { label: 'Explore current industries', href: '/solutions' },
      secondaryCta: { label: 'Talk with AOVIAS', href: '/company/contact#form-contact' },
      illustrationLabel: `${industry.name} — vision direction`,
    },
    overview: {
      heading: framing.overviewHeading,
      description: industry.description,
      opportunities: framing.opportunities,
    },
    challenges: {
      heading: framing.challengesHeading,
      description: framing.challengesDescription,
      items: framing.challenges.map((item, index) => ({
        id: `${industry.slug}-challenge-${index + 1}`,
        title: item.title,
        description: item.description,
      })),
    },
    solutions: {
      heading: framing.solutionsHeading,
      description: framing.solutionsDescription,
      items: framing.solutions.map((item, index) => ({
        id: `${industry.slug}-solution-${index + 1}`,
        heading: item.heading,
        description: item.description,
      })),
    },
    products: {
      heading: 'Likely product fit',
      description: framing.productsDescription,
      productIds: industry.recommendedProductIds,
    },
    benefits: {
      heading: framing.benefitsHeading,
      items: framing.benefits.map((item, index) => ({
        id: `${industry.slug}-benefit-${index + 1}`,
        heading: item.heading,
        description: item.description,
      })),
    },
    trust: {
      ...defaultIndustryTrust,
      description: framing.trustDescription,
    },
    resources: {
      heading: 'Related resources',
      description: 'Background on AOVIAS while this industry page matures.',
      items: [
        {
          id: `${industry.slug}-solutions`,
          kind: 'article',
          title: 'Solutions overview',
          description: 'Industries AOVIAS serves today.',
          href: '/solutions',
          category: { id: 'solutions', name: 'Solutions', tone: 'primary' },
        },
        {
          id: `${industry.slug}-products`,
          kind: 'article',
          title: 'Product portfolio',
          description: 'How AOVIAS products fit together as a platform.',
          href: '/products',
          category: { id: 'products', name: 'Products', tone: 'info' },
        },
        {
          id: `${industry.slug}-company`,
          kind: 'article',
          title: 'About AOVIAS',
          description: 'Purpose, philosophy, and engineering culture.',
          href: '/company/contact#form-demo',
          category: { id: 'company', name: 'Company', tone: 'secondary' },
        },
      ],
    },
    primaryCtaBand: {
      title: framing.ctaTitle,
      description: framing.ctaDescription,
      primaryCta: { label: 'Browse industries', href: '/solutions' },
      secondaryCta: { label: 'Contact AOVIAS', href: '/company/contact#form-contact' },
    },
  };
}

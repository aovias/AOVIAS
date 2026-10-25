import {
  GovernanceWorkflow,
  HumanAiCollaboration,
} from '@/components/brand';
import type { TechnologyEditorialContent } from '@/features/technology/editorial/types';

/**
 * Artificial Intelligence Technology Editorial — responsible, measured, human-centered.
 */
export const aiEditorialContent = {
  id: 'ai',
  atmosphere: 'ai-neural-network',
  hero: {
    eyebrow: 'Artificial Intelligence',
    title: 'Human judgment remains central.',
    description:
      'AOVIAS treats intelligence as a governed platform capability. AI assists institutional work. People review, decide, and remain accountable.',
    primaryCta: {
      label: 'Human-in-the-loop',
      href: '/technology/ai/human-in-the-loop',
    },
    secondaryCta: {
      label: 'Responsible AI',
      href: '/technology/ai/responsible',
    },
    illustration: (
      <HumanAiCollaboration className="xl:min-h-[20rem] 2xl:min-h-[24rem]" />
    ),
  },
  principle: {
    eyebrow: 'Core principle',
    title: 'Assistance with accountability.',
    description:
      'Intelligence should clarify work, surface options, and accelerate careful action — never obscure ownership or invent autonomy where institutions require judgment.',
  },
  architecture: {
    eyebrow: 'Architecture',
    title: 'Human → AI → Review → Decision → Accountability.',
    description:
      'The architectural contract is simple: models propose; people approve; records preserve who decided and why.',
    caption: 'Assisted workflows under institutional oversight',
    diagram: (
      <GovernanceWorkflow
        title="Governed intelligence pathway"
        className="mx-auto"
      />
    ),
  },
  story: {
    eyebrow: 'Technical story',
    title: 'How intelligence enters institutional work.',
    description:
      'A measured path from understanding to accountable action.',
    steps: [
      'Understand',
      'Recommend',
      'Review',
      'Approve',
      'Execute',
      'Account',
    ],
  },
  practices: {
    eyebrow: 'AI practice',
    title: 'Principles over spectacle.',
    description:
      'Scientific, transparent, responsible — never marketed as magic.',
    items: [
      {
        id: 'purpose',
        title: 'Purpose-bound use',
        description:
          'Intelligence is applied where institutional outcomes are clear.',
      },
      {
        id: 'transparency',
        title: 'Transparency',
        description:
          'Recommendations remain inspectable by the people who act on them.',
      },
      {
        id: 'oversight',
        title: 'Human oversight',
        description:
          'Review and approval gates stay mandatory for consequential actions.',
      },
      {
        id: 'governance',
        title: 'Governance',
        description:
          'Policy, auditability, and escalation are engineering requirements.',
      },
      {
        id: 'evaluation',
        title: 'Evaluation',
        description:
          'Capability claims stay measured against institutional duty.',
      },
      {
        id: 'containment',
        title: 'Containment',
        description:
          'Boundaries limit blast radius when assistance is wrong or incomplete.',
      },
    ],
  },
  workflow: {
    eyebrow: 'Workflow',
    title: 'Understand → Recommend → Review → Approve → Execute',
    description:
      'The operating rhythm for assisted institutional work.',
    steps: ['Understand', 'Recommend', 'Review', 'Approve', 'Execute'],
  },
  highlights: {
    eyebrow: 'Capabilities',
    title: 'Where intelligence strengthens the platform.',
    description:
      'Outcome-oriented assistance — not autonomous decision engines.',
    items: [
      {
        id: 'sense',
        title: 'Sense-making',
        description:
          'Structure signals and context so operators can prioritize judgment.',
      },
      {
        id: 'recommend',
        title: 'Recommendations',
        description:
          'Propose next actions with enough context for careful review.',
      },
      {
        id: 'draft',
        title: 'Drafting support',
        description:
          'Accelerate authoring while preserving human authorship and standards.',
      },
      {
        id: 'triage',
        title: 'Triage assistance',
        description:
          'Help teams route work without removing ownership of the decision.',
      },
      {
        id: 'explain',
        title: 'Explainability',
        description:
          'Surfaces that keep rationale visible to reviewers and auditors.',
      },
      {
        id: 'govern',
        title: 'Governed orchestration',
        description:
          'Shared platform controls for where and how assistance is applied.',
      },
    ],
  },
  closing: {
    title: 'Intelligence that institutions can stand behind.',
    description:
      'Continue into principles, human-in-the-loop design, and responsible AI practice.',
    primaryCta: {
      label: 'Explore AI principles',
      href: '/technology/ai/principles',
    },
  },
} as const satisfies TechnologyEditorialContent;

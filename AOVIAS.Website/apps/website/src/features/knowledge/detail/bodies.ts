import { getKnowledgeItemById } from '@/features/knowledge/domain';
import {
  buildTocFromBody,
  type KnowledgeDetailContent,
} from '@/features/knowledge/detail/types';

function detail(
  id: string,
  slug: string,
  body: KnowledgeDetailContent['body'],
  options?: {
    keywords?: readonly string[];
    relatedIds?: readonly string[];
  },
): KnowledgeDetailContent {
  const item = getKnowledgeItemById(id);
  if (!item) {
    throw new Error(`Missing knowledge catalog item: ${id}`);
  }
  return {
    item,
    slug,
    seo: {
      title: item.title,
      description: item.description ?? item.title,
      ...(options?.keywords ? { keywords: options.keywords } : {}),
    },
    body,
    toc: buildTocFromBody(body),
    ...(options?.relatedIds ? { relatedIds: options.relatedIds } : {}),
  };
}

/**
 * Authored detail bodies for knowledge pages — configuration, not page structure.
 * Section shape: problem → principle → practice → close.
 */
export const knowledgeDetails: Record<string, KnowledgeDetailContent> = {
  'institutional-trust': detail(
    'blog-institutional-trust',
    'institutional-trust',
    [
      {
        id: 'problem',
        heading: 'Why institutional trust is an engineering problem',
        paragraphs: [
          'Institutions that society depends on cannot treat trust as a marketing message. Trust is earned through systems that remain operable, inspectable, and accountable over long periods.',
          'When ownership is unclear, claims outrun evidence, or architecture cannot be explained under review, confidence erodes — even if the interface looks polished.',
        ],
      },
      {
        id: 'principle',
        heading: 'Trust as a product requirement',
        paragraphs: [
          'AOVIAS treats engineering discipline, clear ownership, and measured claims as product requirements — not optional polish after launch.',
          'Systems should be explainable to oversight. Interfaces should make responsibility visible. Intelligence capabilities should strengthen accountable human judgment, not obscure it.',
        ],
      },
      {
        id: 'practice',
        heading: 'Practices that compound credibility',
        paragraphs: [
          'Prefer continuity over novelty cycles. Document decisions so successors inherit context. Keep interfaces coherent across releases. Avoid urgency that pressures teams into opaque shortcuts.',
          'When new capability is introduced, name the intended use, the owner of outcomes, and the evidence available for review. Capability without ownership is not progress.',
        ],
      },
      {
        id: 'close',
        heading: 'What good looks like',
        paragraphs: [
          'Leaders can explain how work moves. Teams can see who owns outcomes. Oversight can evaluate systems without decoding theater. That is the standard AOVIAS builds toward.',
        ],
      },
    ],
    {
      keywords: ['institutional trust', 'engineering', 'AOVIAS'],
      relatedIds: [
        'blog-release-discipline',
        'insight-responsible-ai',
        'resource-evaluation-checklist',
      ],
    },
  ),
  'release-discipline': detail(
    'blog-release-discipline',
    'release-discipline',
    [
      {
        id: 'problem',
        heading: 'The cost of artificial urgency',
        paragraphs: [
          'Release cadence should serve institutional continuity. Artificial urgency rarely improves quality and often obscures ownership, risk, and readiness.',
          'When teams ship to satisfy spectacle — announcements, internal pressure, or novelty cycles — institutions inherit debt that is hard to unwind under operational load.',
        ],
      },
      {
        id: 'principle',
        heading: 'Continuity before spectacle',
        paragraphs: [
          'A disciplined release posture defines what “done” means before momentum takes over. Small, reviewable changes beat opaque batches. Honest communication about risk builds more trust than confident theater.',
          'AOVIAS prefers steady delivery that operators can absorb — not launches that force institutions to relearn the system every quarter.',
        ],
      },
      {
        id: 'practice',
        heading: 'How disciplined teams ship',
        paragraphs: [
          'Establish release criteria that include ownership, rollback posture, and documentation updates — not only feature completion. Prefer incremental paths that preserve operability.',
          'Communicate changes in plain language: what changed, who is affected, what to verify. Treat silence after a release as a risk, not a virtue.',
        ],
      },
      {
        id: 'close',
        heading: 'Calm is a quality signal',
        paragraphs: [
          'Institutions notice when release practice respects their duty to stay continuous. Discipline without artificial urgency is how durable software earns the right to run in high-stakes environments.',
        ],
      },
    ],
    {
      keywords: ['release management', 'engineering discipline'],
      relatedIds: [
        'blog-institutional-trust',
        'doc-studio-delivery',
        'release-2026-07',
      ],
    },
  ),
  'responsible-ai': detail(
    'insight-responsible-ai',
    'responsible-ai',
    [
      {
        id: 'problem',
        heading: 'High stakes require restraint',
        paragraphs: [
          'In regulated and high-stakes environments, intelligence features must be evaluated for accountability, not novelty.',
          'The question is not only what a model can do — it is who remains responsible when it is wrong, incomplete, or misapplied, and whether evidence exists for oversight to inspect.',
        ],
      },
      {
        id: 'principle',
        heading: 'Purpose, ownership, and evidence',
        paragraphs: [
          'Responsible adoption starts with intended use. Bound the problem. Name the human owner of outcomes. Require auditability where duty demands it.',
          'AOVIAS applies automation and AI where they improve accountable human judgment — with clear purpose and oversight. Opaque claims that cannot be inspected are not progress.',
        ],
      },
      {
        id: 'practice',
        heading: 'Evaluation principles in practice',
        paragraphs: [
          'Document intended use and out-of-scope uses. Preserve logs and decision context so reviews can reconstruct what happened. Keep humans in the decision path where institutional duty requires it.',
          'Separate evaluation guidance from production commitments. Prefer “supports” and “designed for” over guarantees that cannot be kept. Reject fear-based urgency as a substitute for diligence.',
        ],
      },
      {
        id: 'close',
        heading: 'Accountability cannot be deferred',
        paragraphs: [
          'Institutions that adopt intelligence capabilities without ownership inherit risk they cannot explain. Restraint, clarity, and measurable claims are how responsible AI earns a place in regulated work.',
        ],
      },
    ],
    {
      keywords: ['responsible AI', 'governance'],
      relatedIds: [
        'resource-ai-adoption-guide',
        'blog-institutional-trust',
        'insight-operational-clarity',
      ],
    },
  ),
  'operational-clarity': detail(
    'insight-operational-clarity',
    'operational-clarity',
    [
      {
        id: 'problem',
        heading: 'The cost of ambiguous operations',
        paragraphs: [
          'When teams cannot see ownership and status clearly, institutions pay in delay, risk, and eroded confidence.',
          'Dashboard theater — dense charts without attributable action — creates the appearance of control while leaving handoffs fragile and accountability vague.',
        ],
      },
      {
        id: 'principle',
        heading: 'Clarity as infrastructure',
        paragraphs: [
          'Operational clarity is not an aesthetic preference. It is structured visibility, attributable actions, and durable handoffs that survive staffing changes.',
          'AOVIAS Command is built around that principle: operators should know what is happening, who owns the next step, and what evidence supports oversight conversations.',
        ],
      },
      {
        id: 'practice',
        heading: 'Building clarity into daily work',
        paragraphs: [
          'Define ownership boundaries before tooling expands. Prefer status that drives action over metrics that only decorate. Preserve context in handoffs so successors inherit intent, not archaeology.',
          'Review workflows for places where “someone” owns the outcome. Replace anonymous queues with named accountability. Measure whether teams can explain the path of work without a meeting.',
        ],
      },
      {
        id: 'close',
        heading: 'Advantage without spectacle',
        paragraphs: [
          'Institutions that invest in operational clarity move with less friction and more confidence. Visibility and ownership outperform theater — especially when stakes are high and time is short.',
        ],
      },
    ],
    {
      keywords: ['operations', 'clarity', 'AOVIAS Command'],
      relatedIds: [
        'doc-command-getting-started',
        'insight-responsible-ai',
        'resource-evaluation-checklist',
      ],
    },
  ),
  'platform-overview': detail(
    'doc-platform-overview',
    'platform-overview',
    [
      {
        id: 'problem',
        heading: 'Why orientation matters',
        paragraphs: [
          'Institutional teams evaluating platform software need a clear map: which product does which job, how pieces relate, and where evaluation should begin.',
          'Without orientation, product pages and documentation become a pile of claims instead of a coherent operating model.',
        ],
      },
      {
        id: 'principle',
        heading: 'One platform, distinct jobs',
        paragraphs: [
          'AOVIAS products share engineering standards for security, identity, and interoperability. Each product still has a distinct job.',
          'AOVIAS Command is the operational control and institutional operations workspace. AOVIAS Studio is the delivery workspace to design, refine, and ship institutional work. Do not swap their value propositions.',
        ],
      },
      {
        id: 'practice',
        heading: 'Where to begin',
        paragraphs: [
          'Start with Command when the primary need is structured visibility and accountable operational control. Start with Studio when the primary need is coherent solution design and delivery discipline.',
          'Use this documentation set for implementation context, then review packaging and demonstration paths with AOVIAS. Prefer depth in a focused pilot over breadth that obscures ownership.',
        ],
      },
      {
        id: 'close',
        heading: 'Next steps',
        paragraphs: [
          'Continue with Getting started with AOVIAS Command and Studio delivery practices. Pair documentation with product pages when you need capability detail, and with Contact when you are ready to evaluate fit for your institution.',
        ],
      },
    ],
    {
      keywords: ['AOVIAS platform', 'documentation', 'Command', 'Studio'],
      relatedIds: [
        'doc-command-getting-started',
        'doc-studio-delivery',
        'resource-evaluation-checklist',
      ],
    },
  ),
  'command-getting-started': detail(
    'doc-command-getting-started',
    'command-getting-started',
    [
      {
        id: 'problem',
        heading: 'The adoption trap',
        paragraphs: [
          'Institutions often expand tooling before clarifying critical workflows and ownership boundaries. Early breadth without focus creates noise instead of control.',
          'Command is most valuable when teams know which operational paths must become visible and attributable first.',
        ],
      },
      {
        id: 'principle',
        heading: 'Purpose of AOVIAS Command',
        paragraphs: [
          'AOVIAS Command helps institutional teams operate with structured visibility and accountable control. It is designed for operators and operations teams who need clarity under real workload — not dashboard decoration.',
          'Evaluation should ask whether the workspace strengthens judgment and handoffs, not whether it maximizes unused features.',
        ],
      },
      {
        id: 'practice',
        heading: 'Evaluation and early adoption',
        paragraphs: [
          'Identify critical workflows, ownership boundaries, and integration constraints before pilot design. Prefer clarity over breadth in the first phase.',
          'Define success in operational terms: can teams see status, attribute actions, and preserve context across shifts? Document what is in scope for the pilot and what is deferred. Involve the people who will operate the system daily.',
        ],
      },
      {
        id: 'close',
        heading: 'Moving forward',
        paragraphs: [
          'When the pilot criteria are clear, review the Platform overview for how Command relates to Studio, then talk with AOVIAS to align demonstration and packaging with your institution’s constraints.',
        ],
      },
    ],
    {
      keywords: ['AOVIAS Command', 'getting started', 'operations'],
      relatedIds: [
        'doc-platform-overview',
        'insight-operational-clarity',
        'doc-studio-delivery',
      ],
    },
  ),
  'studio-delivery': detail(
    'doc-studio-delivery',
    'studio-delivery',
    [
      {
        id: 'problem',
        heading: 'When delivery loses coherence',
        paragraphs: [
          'Institutional work often spans teams, vendors, and ownership changes. Without shared standards and preserved context, quality drifts and successors inherit archaeology instead of intent.',
          'Delivery tools that optimize for speed alone leave institutions unable to explain how a solution evolved — or who owns the next change.',
        ],
      },
      {
        id: 'principle',
        heading: 'Studio’s delivery model',
        paragraphs: [
          'AOVIAS Studio supports consistent solution design and delivery so institutional work remains coherent across teams and time.',
          'The principle is stewardship: standards stay shared, handoffs keep context, and quality drift is treated as operational risk — not an aesthetic preference.',
        ],
      },
      {
        id: 'practice',
        heading: 'Recommended practices',
        paragraphs: [
          'Keep design and delivery standards visible to everyone who touches the work. Capture decisions that affect operability. Prefer reviewable increments that preserve institutional continuity.',
          'When ownership changes, transfer context deliberately: purpose, constraints, open risks, and verification expectations. Pair Studio practice with Command when operational visibility must follow what was shipped.',
        ],
      },
      {
        id: 'close',
        heading: 'Coherence over time',
        paragraphs: [
          'Studio exists so institutions can design, refine, and ship work without losing the thread. Continuity of intent is how delivery earns trust after the launch moment passes.',
        ],
      },
    ],
    {
      keywords: ['AOVIAS Studio', 'delivery', 'stewardship'],
      relatedIds: [
        'doc-platform-overview',
        'blog-release-discipline',
        'doc-command-getting-started',
      ],
    },
  ),
  'responsible-ai-guide': detail(
    'resource-ai-adoption-guide',
    'responsible-ai-guide',
    [
      {
        id: 'problem',
        heading: 'Adoption without a framework',
        paragraphs: [
          'Institutions face pressure to adopt AI capabilities quickly. Without a measured framework, teams risk unclear purpose, weak oversight, and claims that cannot survive review.',
          'The cost of rushed adoption is not only technical debt — it is eroded trust when outcomes cannot be explained.',
        ],
      },
      {
        id: 'principle',
        heading: 'A measured adoption framework',
        paragraphs: [
          'Use this guide to evaluate AI capabilities against institutional duty: purpose, accountability, evidence, and operational fit.',
          'Prefer restraint where duty requires human judgment. Prefer inspectable systems over opaque assurances. Separate what is suitable for evaluation from what is ready for production commitment.',
        ],
      },
      {
        id: 'practice',
        heading: 'Checklist themes to apply',
        paragraphs: [
          'Intended use — what problem is bounded, and what is explicitly out of scope?',
          'Human oversight — who owns outcomes, and where must judgment remain in the path?',
          'Auditability — what evidence exists to reconstruct decisions under review?',
          'Data boundaries — what may be used, retained, and shared?',
          'Continuity under failure — how does the institution operate when the capability is wrong or unavailable?',
        ],
      },
      {
        id: 'close',
        heading: 'Use with care',
        paragraphs: [
          'This guide supports diligence; it does not replace policy, counsel, or sector-specific regulation. Pair it with the Responsible AI insight and your institution’s governance process before production use.',
        ],
      },
    ],
    {
      keywords: ['responsible AI', 'guide', 'adoption'],
      relatedIds: [
        'insight-responsible-ai',
        'resource-evaluation-checklist',
        'blog-institutional-trust',
      ],
    },
  ),
  'evaluation-checklist': detail(
    'resource-evaluation-checklist',
    'evaluation-checklist',
    [
      {
        id: 'problem',
        heading: 'Why evaluation needs structure',
        paragraphs: [
          'Enterprise evaluation fails when questions stay vague — “Is it secure?” or “Is it modern?” without evidence paths. Institutions need reference questions that surface ownership, interoperability, and operational readiness.',
          'This checklist organizes diligence themes so conversations stay concrete.',
        ],
      },
      {
        id: 'principle',
        heading: 'What diligence should prove',
        paragraphs: [
          'A credible evaluation shows how identity works, how actions are attributed, how data is bounded, and how continuity holds under change.',
          'Prefer answers you can inspect over assurances you must accept. AOVIAS expects institutions to ask hard questions — and answers them with measured claims.',
        ],
      },
      {
        id: 'practice',
        heading: 'Reference questions',
        paragraphs: [
          'Security and access — How is identity integrated? How are actions attributed? What are the institution-scoped boundaries?',
          'Governance and continuity — Who owns outcomes? How are handoffs preserved? What evidence supports oversight conversations?',
          'Interoperability — Which integration paths are supported today, and which are evaluation guidance versus production commitments?',
          'Operations — Can operators act with clarity under load? What happens when a dependency fails?',
        ],
      },
      {
        id: 'close',
        heading: 'After the checklist',
        paragraphs: [
          'Use answers to decide fit for a focused pilot — not to force a full-scale commitment before ownership is clear. Continue with Platform overview and product documentation, then request a demo when evaluation criteria are ready.',
        ],
      },
    ],
    {
      keywords: ['enterprise evaluation', 'checklist', 'governance'],
      relatedIds: [
        'doc-platform-overview',
        'resource-ai-adoption-guide',
        'insight-operational-clarity',
      ],
    },
  ),
  'platform-notes-2026-07': detail(
    'release-2026-07',
    'platform-notes-2026-07',
    [
      {
        id: 'problem',
        heading: 'What this note covers',
        paragraphs: [
          'Institutions following AOVIAS public materials need calm, accurate updates — not release theater. This note summarizes measured progress across documentation and the public website.',
        ],
      },
      {
        id: 'principle',
        heading: 'Continuity of public materials',
        paragraphs: [
          'Public foundations should stay coherent as products and knowledge expand. Titles, descriptions, and documentation paths should match how Command and Studio are described elsewhere on the site.',
        ],
      },
      {
        id: 'practice',
        heading: 'July 2026 updates',
        paragraphs: [
          'Expanded public website foundations across products, industries, and knowledge experiences.',
          'Aligned documentation and knowledge articles so Blog, Insights, Documentation, and Resources share consistent navigation and search.',
          'Strengthened Trust & Governance documents as public drafts prepared for counsel review.',
        ],
      },
      {
        id: 'close',
        heading: 'Looking ahead',
        paragraphs: [
          'Further notes will continue this measured cadence. For capability detail, use product documentation; for governance, use the Trust & Governance center. No release dates or SLAs are implied beyond what is stated here.',
        ],
      },
    ],
    {
      keywords: ['release notes', 'AOVIAS', 'platform'],
      relatedIds: [
        'doc-platform-overview',
        'blog-release-discipline',
        'doc-command-getting-started',
      ],
    },
  ),
};

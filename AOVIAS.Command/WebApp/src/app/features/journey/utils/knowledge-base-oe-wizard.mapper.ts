import { NodeDetailsDto, KnowledgeItemDto, RequirementDetailDto } from '../models/knowledge-base-api.model';
import { ObjectiveElement } from '../models/objective-element.model';

/**
 * @see ComplaincePlatform/docs/backend/knowledge-base-wizard-ui-binding.md
 * Product (Phase 51-02-e): local wizard slider 1–5 vs API `maxScore` — see `KB_SCORE_SCALE_PRODUCT_NOTE`.
 * Do not bind `RequirementDetailDto.classification` to evaluation-tone radios without a product rule.
 */

export const KB_SCORE_SCALE_PRODUCT_NOTE =
  'Knowledge base `maxScore` / `weight` reflect the scoring model; the wizard evaluation slider stays local 1–5 until mapping is defined.';

export interface OeWizardKbVm {
  explanation: string;
  whyItMatters: string;
  howToComply: string;
  evidenceRequired: string;
  auditChecklist: string;
  nodeCodeLabel?: string;
  nodeTitleLabel?: string;
  kbRequirementMaxScore?: number | null;
  kbRequirementWeight?: number | null;
  requirementIntent?: string;
  requirementMeasurableElements?: string;
  requirementClassification?: string;
  requirementIsMandatory?: boolean | null;
  requirementApplicableDepartments?: string;
  requirementRequiresDocumentation?: boolean | null;
  riskLevel?: string;
  impactLevel?: string;
  tags?: string;
}

function hasKnowledgeText(k: KnowledgeItemDto): boolean {
  return [k.explanation, k.whyItMatters, k.howToComply, k.evidenceRequired, k.auditChecklist].some(
    (s) => typeof s === 'string' && s.trim().length > 0,
  );
}

function hasKnowledgeBadges(k: KnowledgeItemDto): boolean {
  return [k.riskLevel, k.impactLevel, k.tags].some(
    (s) => typeof s === 'string' && s.trim().length > 0,
  );
}

function hasRequirementText(r: RequirementDetailDto): boolean {
  return [r.intent, r.measurableElements, r.classification, r.applicableDepartments].some(
    (s) => typeof s === 'string' && s.trim().length > 0,
  );
}

/** True when `NodeDetailsDto` has anything worth binding per wizard UI guide (knowledge, requirement, node labels, or score). */
export function hasBindableNodeDetailsContent(dto: NodeDetailsDto | null): boolean {
  if (!dto) return false;
  const k = pickKnowledgeItem(dto.knowledge);
  if (k && (hasKnowledgeText(k) || hasKnowledgeBadges(k))) return true;
  if (dto.requirement && hasRequirementText(dto.requirement)) return true;
  if (dto.requirement?.isMandatory === true || dto.requirement?.requiresDocumentation === true) return true;
  if (dto.node?.code?.trim() || dto.node?.title?.trim()) return true;
  if (dto.requirementScore != null) return true;
  return false;
}

/** Prefer active items; tie-break by highest `version`. */
export function pickKnowledgeItem(
  items: KnowledgeItemDto[] | null | undefined,
): KnowledgeItemDto | null {
  if (!items?.length) return null;
  const active = items.filter((k) => k.isActive);
  const pool = active.length ? active : items;
  return [...pool].sort((a, b) => (b.version ?? 0) - (a.version ?? 0))[0] ?? null;
}

export function syntheticNodeDetailsFromKnowledgeItems(
  items: KnowledgeItemDto[],
  _oe: ObjectiveElement,
): NodeDetailsDto | null {
  if (!items?.length) return null;
  return {
    node: null,
    requirement: null,
    knowledge: items,
    requirementScore: null,
  };
}

export function mapNodeDetailsToWizardVm(
  node: NodeDetailsDto | null,
  oe: ObjectiveElement | null,
): OeWizardKbVm | null {
  if (!node || !oe) return null;
  const k = pickKnowledgeItem(node.knowledge);
  const req = node.requirement;

  const hasK = k && (hasKnowledgeText(k) || hasKnowledgeBadges(k));
  const hasReq = req && (hasRequirementText(req) || req.isMandatory === true || req.requiresDocumentation === true);
  const hasMeta =
    !!(node.node?.code?.trim() || node.node?.title?.trim()) || node.requirementScore != null;

  if (!hasK && !hasReq && !hasMeta) return null;

  return {
    explanation: (k?.explanation ?? '').trim(),
    whyItMatters: (k?.whyItMatters ?? '').trim(),
    howToComply: (k?.howToComply ?? '').trim(),
    evidenceRequired: (k?.evidenceRequired ?? '').trim(),
    auditChecklist: (k?.auditChecklist ?? '').trim(),
    nodeCodeLabel: node.node?.code ?? oe.code,
    nodeTitleLabel: node.node?.title ?? oe.title ?? oe.description,
    kbRequirementMaxScore: node.requirementScore?.maxScore ?? null,
    kbRequirementWeight: node.requirementScore?.weight ?? null,
    requirementIntent: (req?.intent ?? '').trim(),
    requirementMeasurableElements: (req?.measurableElements ?? '').trim(),
    requirementClassification: (req?.classification ?? '').trim(),
    requirementIsMandatory: req?.isMandatory ?? null,
    requirementApplicableDepartments: (req?.applicableDepartments ?? '').trim(),
    requirementRequiresDocumentation: req?.requiresDocumentation ?? null,
    riskLevel: (k?.riskLevel ?? '').trim(),
    impactLevel: (k?.impactLevel ?? '').trim(),
    tags: (k?.tags ?? '').trim(),
  };
}

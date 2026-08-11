import { JourneyChapter } from '../models/chapter.model';
import {
  JourneyWorkspaceApiDto,
  ObjectiveElementApiDto,
  JourneyChapterApiDto,
  ComplianceStandardApiDto,
  OeActionPlanApiDto,
  OeEvidenceItemApiDto,
  OeHistoryEventApiDto,
  JourneyTrendPointApiDto,
  JourneySessionDto,
  JourneyDashboardDto,
  JourneyChaptersIndexDto,
  JourneyChapterIndexItemDto,
  JourneyOverviewMetricsApiDto,
  StageProgressApiDto,
  ObligationBandSummaryApiDto,
  ObligationMaturityBreakdownApiDto,
} from '../models/journey-api.model';
import {
  JourneyWorkspaceSnapshot,
  JourneyCatalogMeta,
  JourneyModeProgression,
  JourneyOverviewMetrics,
  JourneyStageProgress,
  ObligationBandSummary,
  ObligationMaturityBreakdown,
} from '../models/journey-tree.model';
import { ComplianceStandard } from '../models/standard.model';
import {
  ActionPlanStatus,
  AuditComplianceStatus,
  OperationalMode,
  ChapterRiskLevel,
  EvidenceStatus,
  NabhScore,
  OeClassification,
  OeHistoryEventType,
  ScoreLabel,
} from '../models/nabh.enums';
import {
  ObjectiveElement,
  OeActionPlan,
  OeEvidenceItem,
  OeHistoryEvent,
} from '../models/objective-element.model';
import {
  mapMaturityStageState,
  normMaturityStageId,
  type MaturityStageStateModel,
} from '../../../shared/models/maturity-stage.model';

function normStr(v: unknown, fallback: string): string {
  if (v === null || v === undefined) return fallback;
  const s = String(v).trim();
  return s.length ? s : fallback;
}

function normRisk(v: unknown): ChapterRiskLevel {
  const s = String(v ?? '').toLowerCase();
  if (s === 'high' || s === 'medium' || s === 'low') return s;
  return 'medium';
}

function normCoreStatus(v: unknown): 'met' | 'at_risk' | 'failed' {
  const s = String(v ?? '').toLowerCase();
  if (s === 'met' || s === 'at_risk' || s === 'failed') return s;
  if (s === 'at risk') return 'at_risk';
  if (s.includes('compliant') && !s.includes('non')) return 'met';
  if (s.includes('fail')) return 'failed';
  return 'at_risk';
}

function normClassification(v: unknown): OeClassification {
  if (typeof v === 'number' && Number.isFinite(v)) {
    const n = Math.round(v);
    if (n === 0) return 'core';
    if (n === 1) return 'commitment';
    if (n === 2) return 'achievement';
    if (n === 3 || n === 4) return 'excellence';
    return 'core';
  }
  const s = String(v ?? '').trim().toLowerCase();
  if (/^[0-4]$/.test(s)) {
    return normClassification(Number(s));
  }
  if (s === 'core' || s === 'commitment' || s === 'achievement' || s === 'excellence') return s;
  return 'core';
}

function clampScore(n: unknown): NabhScore {
  const x = Number(n);
  if (!Number.isFinite(x)) return 3;
  const r = Math.round(x);
  return (Math.min(5, Math.max(1, r)) || 3) as NabhScore;
}

const SCORE_LABELS: ScoreLabel[] = [
  'Poor',
  'Limited',
  'Partial',
  'Substantial',
  'Full Compliance',
];

function normScoreLabel(v: unknown, score: NabhScore): ScoreLabel {
  const s = String(v ?? '').trim();
  if (SCORE_LABELS.includes(s as ScoreLabel)) return s as ScoreLabel;
  return SCORE_LABELS[score - 1] ?? 'Partial';
}

function normEvidenceStatus(v: unknown): EvidenceStatus {
  const s = String(v ?? '').toLowerCase();
  if (s === 'complete' || s === 'missing' || s === 'pending_verification') return s;
  if (s === 'pending') return 'pending_verification';
  if (s.includes('verified') || s.includes('complete')) return 'complete';
  return 'missing';
}

function normAuditCompliance(v: unknown): AuditComplianceStatus {
  const s = String(v ?? '').toLowerCase();
  if (s === 'compliant' || s === 'non_compliant' || s === 'not_applicable') return s;
  if (s === 'non compliant') return 'non_compliant';
  if (s === 'accepted' || s === 'verified') return 'compliant';
  return 'non_compliant';
}

function normVerification(v: unknown): 'verified' | 'pending' | 'rejected' {
  if (typeof v === 'number' && Number.isFinite(v)) {
    const n = Math.round(v);
    if (n === 1) return 'verified';
    if (n === 2) return 'rejected';
    return 'pending';
  }
  const raw = String(v ?? '').trim().toLowerCase();
  if (raw === '1') return 'verified';
  if (raw === '2') return 'rejected';
  if (raw === '0' || raw === '') return 'pending';
  if (raw === 'verified' || raw === 'pending' || raw === 'rejected') return raw;
  if (raw.includes('verify')) return 'verified';
  if (raw.includes('reject')) return 'rejected';
  return 'pending';
}

function normHistoryType(v: unknown): OeHistoryEventType {
  const s = String(v ?? '').toLowerCase();
  if (s === 'audit' || s === 'evidence' || s === 'task' || s === 'action_plan') return s;
  return 'audit';
}

function normActionPlanStatus(v: unknown): ActionPlanStatus {
  const s = String(v ?? '').toLowerCase();
  if (s === 'open' || s === 'in_progress' || s === 'closed') return s;
  if (s === 'in progress') return 'in_progress';
  return 'open';
}

/** Parses API values: integers 0–4, enum names, and legacy snake_case strings. */
export function normOperationalMode(v: unknown): OperationalMode | null {
  if (typeof v === 'number' && Number.isInteger(v) && v >= 0 && v <= 4) {
    return v as OperationalMode;
  }
  const raw = String(v ?? '').trim();
  if (/^-?\d+$/.test(raw)) {
    const n = Number.parseInt(raw, 10);
    if (n >= 0 && n <= 4) return n as OperationalMode;
  }
  const compact = raw.replace(/[-_\s]/g, '').toLowerCase();
  const map: Record<string, OperationalMode> = {
    selfassessment: OperationalMode.SelfAssessment,
    internalaudit: OperationalMode.InternalAudit,
    finalaudit: OperationalMode.FinalAudit,
    surveillance: OperationalMode.Surveillance,
    reaccreditation: OperationalMode.ReAccreditation,
  };
  return map[compact] ?? null;
}

export function mapOverviewMetricsApiToDomain(
  ov: JourneyOverviewMetricsApiDto | null | undefined,
): JourneyOverviewMetrics {
  return {
    coreCompliancePercent:
      typeof ov?.coreCompliancePercent === 'number' && Number.isFinite(ov.coreCompliancePercent)
        ? ov.coreCompliancePercent
        : 0,
    failedCoreCount:
      typeof ov?.failedCoreCount === 'number' && Number.isFinite(ov.failedCoreCount)
        ? ov.failedCoreCount
        : 0,
    oesAtOrBelowThreeCount:
      typeof ov?.oesAtOrBelowThreeCount === 'number' && Number.isFinite(ov.oesAtOrBelowThreeCount)
        ? ov.oesAtOrBelowThreeCount
        : 0,
    openActionPlansCount:
      typeof ov?.openActionPlansCount === 'number' && Number.isFinite(ov.openActionPlansCount)
        ? ov.openActionPlansCount
        : 0,
    stageBreakdown: mapStageBreakdown(ov?.stageBreakdown),
    // Obligation enrichment (Phase 4)
    obligationBreakdown: mapObligationBreakdown(ov?.obligationBreakdown),
    obligationReadinessScore:
      typeof ov?.obligationReadinessScore === 'number' ? ov.obligationReadinessScore : undefined,
  };
}

function mapObligationBandSummary(
  b: ObligationBandSummaryApiDto | null | undefined,
): ObligationBandSummary {
  return {
    total:         typeof b?.total         === 'number' ? b.total         : 0,
    verified:      typeof b?.verified      === 'number' ? b.verified      : 0,
    implemented:   typeof b?.implemented   === 'number' ? b.implemented   : 0,
    partial:       typeof b?.partial       === 'number' ? b.partial       : 0,
    deficient:     typeof b?.deficient     === 'number' ? b.deficient     : 0,
    notStarted:    typeof b?.notStarted    === 'number' ? b.notStarted    : 0,
    unknown:       typeof b?.unknown       === 'number' ? b.unknown       : 0,
    readinessScore: typeof b?.readinessScore === 'number' ? b.readinessScore : 0,
  };
}

function mapObligationBreakdown(
  bd: ObligationMaturityBreakdownApiDto | null | undefined,
): ObligationMaturityBreakdown | null {
  if (!bd) return null;
  return {
    core:        mapObligationBandSummary(bd.core),
    commitment:  mapObligationBandSummary(bd.commitment),
    achievement: mapObligationBandSummary(bd.achievement),
    excellence:  mapObligationBandSummary(bd.excellence),
  };
}

function mapStageBreakdown(
  items: StageProgressApiDto[] | null | undefined,
): JourneyStageProgress[] {
  if (!Array.isArray(items) || items.length === 0) return [];
  const validStages = ['core', 'commitment', 'achievement', 'excellence'] as const;
  return items
    .filter((it) => validStages.includes((it.stage ?? '').toLowerCase() as (typeof validStages)[number]))
    .map((it) => ({
      stage: (it.stage ?? '').toLowerCase() as JourneyStageProgress['stage'],
      label: it.label ?? '',
      score: typeof it.score === 'number' && Number.isFinite(it.score) ? it.score : 0,
      totalOeCount: typeof it.totalOeCount === 'number' ? it.totalOeCount : 0,
      scoredOeCount: typeof it.scoredOeCount === 'number' ? it.scoredOeCount : 0,
      // Obligation enrichment — Phase 4
      totalObligations: typeof it.totalObligations === 'number' ? it.totalObligations : undefined,
      verifiedObligations: typeof it.verifiedObligations === 'number' ? it.verifiedObligations : undefined,
      implementedObligations: typeof it.implementedObligations === 'number' ? it.implementedObligations : undefined,
      partialObligations: typeof it.partialObligations === 'number' ? it.partialObligations : undefined,
      deficientObligations: typeof it.deficientObligations === 'number' ? it.deficientObligations : undefined,
      obligationReadinessScore: typeof it.obligationReadinessScore === 'number' ? it.obligationReadinessScore : undefined,
    }));
}

function mapTrend(pts: JourneyTrendPointApiDto[] | null | undefined): { label: string; value: number }[] {
  if (!Array.isArray(pts)) return [];
  return pts.map((p) => ({
    label: normStr(p?.label, '—'),
    value: typeof p?.value === 'number' && Number.isFinite(p.value) ? p.value : 0,
  }));
}

function mapActionPlan(d: OeActionPlanApiDto): OeActionPlan {
  return {
    id: normStr(d?.id, ''),
    title: normStr(d?.title, '—'),
    ownerName: normStr(d?.ownerName, '—'),
    deadline: normStr(d?.deadline, ''),
    status: normActionPlanStatus(d?.status),
    accepted: d?.accepted === true,
  };
}

function pickEvidenceMime(d: OeEvidenceItemApiDto): string | null {
  const raw = d?.mimeType ?? d?.contentType;
  if (raw === null || raw === undefined) return null;
  const s = String(raw).trim();
  return s.length ? s : null;
}

function pickEvidenceFileUrl(d: OeEvidenceItemApiDto): string | null {
  const raw = d?.fileUrl;
  if (raw === null || raw === undefined) return null;
  const s = String(raw).trim();
  return s.length ? s : null;
}

export function mapEvidenceItemApiToDomain(d: OeEvidenceItemApiDto): OeEvidenceItem {
  const mime = pickEvidenceMime(d);
  const fileUrl = pickEvidenceFileUrl(d);
  const fileNameRaw = d?.fileName;
  const fileName =
    fileNameRaw != null && String(fileNameRaw).trim().length ? String(fileNameRaw).trim() : null;
  return {
    id: normStr(d?.id, ''),
    name: normStr(d?.name, '—'),
    ...(fileName ? { fileName } : {}),
    ...(mime ? { mimeType: mime } : {}),
    ...(fileUrl ? { fileUrl } : {}),
    verificationStatus: normVerification(d?.verificationStatus),
    uploadedAt: normStr(d?.uploadedAt, ''),
    expiryAt: d?.expiryAt == null || d.expiryAt === '' ? null : String(d.expiryAt),
    uploadedBy: normStr(d?.uploadedBy, '—'),
  };
}

function mapHistoryEvent(d: OeHistoryEventApiDto): OeHistoryEvent {
  return {
    id: normStr(d?.id, ''),
    type: normHistoryType(d?.type),
    title: normStr(d?.title, '—'),
    description: normStr(d?.description, ''),
    timestamp: normStr(d?.timestamp, ''),
    actor: normStr(d?.actor, '—'),
  };
}

export function mapHistoryEventApiToDomain(d: OeHistoryEventApiDto): OeHistoryEvent {
  return mapHistoryEvent(d);
}

export function mapObjectiveElementApiToDomain(d: ObjectiveElementApiDto): ObjectiveElement {
  const score = clampScore(d?.score);
  const title = d?.title?.trim();
  const desc = normStr(d?.description, '');
  return {
    id: normStr(d?.id, ''),
    standardId: normStr(d?.standardId, ''),
    code: normStr(d?.code, '—'),
    title: title || undefined,
    description: desc.length ? desc : normStr(title, '—'),
    classification: normClassification(d?.classification),
    score,
    scoreLabel: normScoreLabel(d?.scoreLabel, score),
    scoreRationale: d?.scoreRationale == null ? undefined : String(d.scoreRationale),
    evidenceStatus: normEvidenceStatus(d?.evidenceStatus),
    ...(d?.evidenceVerifiedFlag === true ? { evidenceVerifiedFlag: true } : {}),
    isNa: !!d?.isNa,
    naJustification: d?.naJustification == null ? undefined : String(d.naJustification),
    auditStatus: normAuditCompliance(d?.auditStatus),
    departmentLabel: d?.departmentLabel == null ? undefined : String(d.departmentLabel),
    assigneeDisplayName: d?.assigneeDisplayName == null ? undefined : String(d.assigneeDisplayName),
    lastUpdatedAt: d?.lastUpdatedAt == null ? undefined : String(d.lastUpdatedAt),
    dueAt: d?.dueAt == null ? null : String(d.dueAt),
    nextReviewAt: d?.nextReviewAt == null ? null : String(d.nextReviewAt),
    actionPlans: Array.isArray(d?.actionPlans) ? d.actionPlans.map(mapActionPlan) : [],
    evidence: Array.isArray(d?.evidence) ? d.evidence.map(mapEvidenceItemApiToDomain) : [],
    history: Array.isArray(d?.history) ? d.history.map(mapHistoryEventApiToDomain) : [],
    auditRemarks: d?.auditRemarks == null ? undefined : String(d.auditRemarks),
    version: typeof d?.version === 'number' && Number.isFinite(d.version) ? d.version : undefined,
    journeyWorkflowStatus:
      d?.journeyWorkflowStatus == null ? undefined : String(d.journeyWorkflowStatus),
    changedSinceLastAudit: d?.changedSinceLastAudit ?? null,
    excellenceNote: d?.excellenceNote == null ? undefined : String(d.excellenceNote),
    obligationState: d?.obligationState ?? null,
  };
}

export function mapComplianceStandardDto(d: ComplianceStandardApiDto, chapterId: string): ComplianceStandard {
  const sid = normStr(d?.id, '');
  const rawOes =
    d?.objectiveElements ??
    (d as { ObjectiveElements?: ObjectiveElementApiDto[] | null }).ObjectiveElements;
  const oes = Array.isArray(rawOes)
    ? rawOes.map((oe) => ({
        ...mapObjectiveElementApiToDomain(oe),
        standardId: normStr(oe?.standardId, sid),
      }))
    : [];
  return {
    id: sid,
    chapterId: normStr(d?.chapterId, chapterId),
    code: normStr(d?.code, '—'),
    title: normStr(d?.title ?? d?.name ?? d?.code, '—'),
    description: normStr(d?.description, ''),
    completionPercent:
      typeof d?.completionPercent === 'number' && Number.isFinite(d.completionPercent)
        ? Math.min(100, Math.max(0, d.completionPercent))
        : 0,
    riskBadge: normRisk(d?.riskBadge),
    coreSummary: d?.coreSummary == null ? undefined : String(d.coreSummary),
    objectiveElements: oes,
  };
}

function mapStandard(d: ComplianceStandardApiDto, chapterId: string): ComplianceStandard {
  return mapComplianceStandardDto(d, chapterId);
}

function mapChapter(d: JourneyChapterApiDto): JourneyChapter {
  const chId = normStr(d?.id, '');
  const standards = Array.isArray(d?.standards) ? d.standards.map((s) => mapStandard(s, chId)) : [];
  return {
    id: normStr(d?.id, ''),
    code: normStr(d?.code, '—'),
    name: normStr(d?.name, ''),
    complianceScorePercent:
      typeof d?.complianceScorePercent === 'number' && Number.isFinite(d.complianceScorePercent)
        ? Math.min(100, Math.max(0, d.complianceScorePercent))
        : 0,
    risk: normRisk(d?.risk),
    coreComplianceStatus: normCoreStatus(d?.coreComplianceStatus),
    openIssuesCount:
      typeof d?.openIssuesCount === 'number' && Number.isFinite(d.openIssuesCount)
        ? d.openIssuesCount
        : 0,
    pendingAudit: !!d?.pendingAudit,
    coreFailureFlag: !!d?.coreFailureFlag,
    standards,
  };
}

export function mapApiToWorkspaceSnapshot(raw: JourneyWorkspaceApiDto): JourneyWorkspaceSnapshot {
  const chapters = Array.isArray(raw?.chapters) ? raw.chapters.map(mapChapter) : [];
  const acc = raw?.accreditation;
  const ov = raw?.overview;

  const modeRaw = raw?.operationalMode ?? raw?.auditLifecycleMode ?? raw?.lifecycleMode;
  const lifecycleMode = normOperationalMode(modeRaw) ?? undefined;

  return {
    journeyId: normStr(raw?.journeyId, ''),
    frameworkCode: normStr(raw?.frameworkCode, ''),
    frameworkName: normStr(raw?.frameworkName, ''),
    chapters,
    accreditation: {
      overallCompliancePercent:
        typeof acc?.overallCompliancePercent === 'number' && Number.isFinite(acc.overallCompliancePercent)
          ? acc.overallCompliancePercent
          : 0,
      coreOeAverageScore:
        typeof acc?.coreOeAverageScore === 'number' && Number.isFinite(acc.coreOeAverageScore)
          ? acc.coreOeAverageScore
          : 0,
      averageStandardScore:
        typeof acc?.averageStandardScore === 'number' && Number.isFinite(acc.averageStandardScore)
          ? acc.averageStandardScore
          : 0,
      averageChapterScore:
        typeof acc?.averageChapterScore === 'number' && Number.isFinite(acc.averageChapterScore)
          ? acc.averageChapterScore
          : 0,
      violations: Array.isArray(acc?.violations) ? acc.violations.map((x) => String(x)) : [],
    },
    overview: {
      coreCompliancePercent:
        typeof ov?.coreCompliancePercent === 'number' && Number.isFinite(ov.coreCompliancePercent)
          ? ov.coreCompliancePercent
          : 0,
      failedCoreCount:
        typeof ov?.failedCoreCount === 'number' && Number.isFinite(ov.failedCoreCount)
          ? ov.failedCoreCount
          : 0,
      oesAtOrBelowThreeCount:
        typeof ov?.oesAtOrBelowThreeCount === 'number' && Number.isFinite(ov.oesAtOrBelowThreeCount)
          ? ov.oesAtOrBelowThreeCount
          : 0,
      openActionPlansCount:
        typeof ov?.openActionPlansCount === 'number' && Number.isFinite(ov.openActionPlansCount)
          ? ov.openActionPlansCount
          : 0,
      stageBreakdown: mapStageBreakdown(ov?.stageBreakdown),
    },
    oeScoreTrend: mapTrend(raw?.oeScoreTrend),
    chapterImprovementTrend: mapTrend(raw?.chapterImprovementTrend),
    riskEvolutionTrend: mapTrend(raw?.riskEvolutionTrend),
    lifecycleMode,
    readOnly: raw?.readOnly === true,
    lastSyncedAt:
      typeof raw?.lastSyncedAt === 'string' && raw.lastSyncedAt.trim() ? raw.lastSyncedAt.trim() : undefined,
  };
}

function mapCatalogApiToDomain(c: JourneySessionDto['catalog']): JourneyCatalogMeta | undefined {
  if (!c) return undefined;
  return {
    standardEdition: normStr(c.standardEdition, ''),
    effectiveFrom: normStr(c.effectiveFrom, ''),
    totalStandardsExpected:
      typeof c.totalStandardsExpected === 'number' && Number.isFinite(c.totalStandardsExpected)
        ? c.totalStandardsExpected
        : 0,
    totalOesExpected:
      typeof c.totalOesExpected === 'number' && Number.isFinite(c.totalOesExpected) ? c.totalOesExpected : 0,
    isPartialCatalog: c.isPartialCatalog === true,
  };
}

function mapModeProgressionApiToDomain(
  m: JourneySessionDto['modeProgression'],
): JourneyModeProgression | undefined {
  if (!m) return undefined;
  const allowed = Array.isArray(m.allowedNextModes)
    ? m.allowedNextModes.filter((x) => typeof x === 'number' && x >= 0 && x <= 4)
    : [];
  const locks = m.lockReasonByTargetMode;
  const lockReasonByTargetMode: Record<string, string | null> = {};
  if (locks && typeof locks === 'object') {
    for (const k of Object.keys(locks)) {
      const v = locks[k];
      lockReasonByTargetMode[k] = v == null ? null : String(v);
    }
  }
  return { allowedNextModes: allowed, lockReasonByTargetMode };
}

/** Map GET …/chapters `items[]` row — standards loaded separately. */
export function mapChapterIndexItemToJourneyChapter(d: JourneyChapterIndexItemDto): JourneyChapter {
  return {
    id: normStr(d?.id, ''),
    code: normStr(d?.code, '—'),
    name: normStr(d?.name, ''),
    complianceScorePercent:
      typeof d?.complianceScorePercent === 'number' && Number.isFinite(d.complianceScorePercent)
        ? Math.min(100, Math.max(0, d.complianceScorePercent))
        : 0,
    risk: normRisk(d?.risk),
    coreComplianceStatus: normCoreStatus(d?.coreComplianceStatus),
    openIssuesCount:
      typeof d?.openIssuesCount === 'number' && Number.isFinite(d.openIssuesCount) ? d.openIssuesCount : 0,
    pendingAudit: !!d?.pendingAudit,
    coreFailureFlag: !!d?.coreFailureFlag,
    standards: [],
    issueBadgeLabel: d?.issueBadgeLabel == null ? undefined : String(d.issueBadgeLabel),
    passFailLabel: d?.passFailLabel == null ? undefined : String(d.passFailLabel),
    changedSinceLastAudit: d?.changedSinceLastAudit ?? null,
    maturityLabel: d?.maturityLabel == null ? undefined : String(d.maturityLabel),
    highlightHighRisk: d?.highlightHighRisk === true,
  };
}

/** Merge split GET session + dashboard + chapters index into one workspace snapshot. */
export function mergeJourneyWorkspaceFromSplit(
  journeyId: string,
  session: JourneySessionDto,
  dashboard: JourneyDashboardDto,
  chaptersIndex: JourneyChaptersIndexDto,
): JourneyWorkspaceSnapshot {
  const items = chaptersIndex?.items;
  const chapters = Array.isArray(items) ? items.map(mapChapterIndexItemToJourneyChapter) : [];

  const acc = dashboard?.accreditation;
  const ov = dashboard?.overview;

  const mode = normOperationalMode(session?.operationalMode) ?? undefined;

  return {
    journeyId: normStr(session?.journeyId, journeyId),
    frameworkCode: normStr(session?.frameworkCode, ''),
    frameworkName: normStr(session?.frameworkName, ''),
    catalog: mapCatalogApiToDomain(session?.catalog),
    chapters,
    accreditation: {
      overallCompliancePercent:
        typeof acc?.overallCompliancePercent === 'number' && Number.isFinite(acc.overallCompliancePercent)
          ? acc.overallCompliancePercent
          : 0,
      coreOeAverageScore:
        typeof acc?.coreOeAverageScore === 'number' && Number.isFinite(acc.coreOeAverageScore)
          ? acc.coreOeAverageScore
          : 0,
      averageStandardScore:
        typeof acc?.averageStandardScore === 'number' && Number.isFinite(acc.averageStandardScore)
          ? acc.averageStandardScore
          : 0,
      averageChapterScore:
        typeof acc?.averageChapterScore === 'number' && Number.isFinite(acc.averageChapterScore)
          ? acc.averageChapterScore
          : 0,
      violations: Array.isArray(acc?.violations) ? acc.violations.map((x) => String(x)) : [],
    },
    overview: mapOverviewMetricsApiToDomain(ov),
    oeScoreTrend: mapTrend(dashboard?.oeScoreTrend),
    chapterImprovementTrend: mapTrend(dashboard?.chapterImprovementTrend),
    riskEvolutionTrend: mapTrend(dashboard?.riskEvolutionTrend),
    lifecycleMode: mode,
    readOnly: session?.readOnly === true,
    snapshotReadOnly: session?.snapshotReadOnly === true,
    accreditationReadinessPercent:
      typeof dashboard?.accreditationReadinessPercent === 'number' &&
      Number.isFinite(dashboard.accreditationReadinessPercent)
        ? dashboard.accreditationReadinessPercent
        : null,
    modeProgression: mapModeProgressionApiToDomain(session?.modeProgression),
    lastSyncedAt:
      typeof session?.lastSyncedAt === 'string' && session.lastSyncedAt.trim()
        ? session.lastSyncedAt.trim()
        : undefined,
    maturityStages: mapMaturityStagesFromDashboard(dashboard),
    currentMaturityStage: normMaturityStageId(dashboard?.currentStage ?? null),
  };
}

function mapMaturityStagesFromDashboard(
  dashboard: JourneyDashboardDto,
): MaturityStageStateModel[] {
  const raw = dashboard?.maturityStages;
  if (!Array.isArray(raw) || raw.length === 0) return [];
  return raw.map(mapMaturityStageState).filter((s): s is MaturityStageStateModel => s != null);
}

import { OperationalMode } from './nabh.enums';

/** §3 Dashboard — NABH progress bar presentation variant per OperationalMode. */
export type JourneyProgressBarPresentation =
  | 'completion'
  | 'validation_adjusted'
  | 'audit_readiness'
  | 'delta_vs_last'
  | 'maturity';

/** §7 Wizard — per-step interaction (Understanding → Compliance Guidance → Evidence → Audit Checklist → Review). */
export type WizardStepInteraction = 'active' | 'locked' | 'readonly';

/** §7 Wizard — Evidence block behavior. */
export type WizardEvidenceSectionMode = 'hidden' | 'required' | 'readonly' | 'conditional';

/** §7 Wizard — Tasks block behavior. */
export type WizardTasksSectionMode = 'hidden' | 'enabled' | 'disabled';

/**
 * Centralized UI policy: OperationalMode → visibility, labels, and interaction.
 * Populated by `buildJourneyOperationalModeUiPolicy` (pure).
 */
export interface JourneyOperationalModeUiPolicy {
  mode: OperationalMode;
  /** True when indicators need API fields not yet wired (dev diagnostics only). */
  missingData: boolean;

  /** §3 Dashboard — progress card */
  progressBarPresentation: JourneyProgressBarPresentation;
  progressCardTitle: string;
  progressCardSubtitle: string;

  /** §3 Dashboard — View High Risk OEs */
  showHighRiskOesAction: boolean;
  /** Emphasize danger action (e.g. Final Audit). */
  dangerActionEmphasis: boolean;

  /** §3 Dashboard — metric KPI visibility */
  metrics: {
    coreCompliance: boolean;
    failedCore: boolean;
    lowScore: boolean;
    openActionPlans: boolean;
  };

  /** §4–5 Execution workspace — chapter / standard / OE presentation */
  execution: {
    chapterShowIssueBadge: boolean;
    chapterHighlightHighRisk: boolean;
    chapterShowPassFail: boolean;
    chapterShowChangedSinceAudit: boolean;
    chapterShowMaturityIndicator: boolean;
    standardHideOpenIssuesLine: boolean;
    standardShowEvidenceGaps: boolean;
    standardShowComplianceStatus: boolean;
    standardShowChangeTrend: boolean;
    standardShowPreviouslyFailed: boolean;
    standardShowExcellenceBadge: boolean;
    oeHideRiskLabels: boolean;
    oeShowClassification: boolean;
    oeRowReadOnly: boolean;
    oeShowChangedSinceAudit: boolean;
    oeShowExcellenceDetail: boolean;
  };

  /** §6 Right context panel */
  context: {
    showEvidenceUpload: boolean;
    evidenceUploadDisabled: boolean;
    evidenceUploadDisabledReason: string | null;
    showTasks: boolean;
    tasksDisabled: boolean;
    showKpiInsights: boolean;
  };

  /** §7 OE execution wizard */
  wizard: {
    stepInteractions: WizardStepInteraction[];
    evidenceSection: WizardEvidenceSectionMode;
    tasksSection: WizardTasksSectionMode;
    evaluateRequiresEvidence: boolean;
    surveillanceEvaluateOnlyIfChanged: boolean;
    allStepsReadOnly: boolean;
    blockStepNavigation: boolean;
  };
}

/** Inputs for pure policy (no Angular). */
export interface JourneyOperationalModePolicyContext {
  snapshotReadOnly: boolean;
  /** Lifecycle + snapshot read-only (Final/Surveillance/Re-accreditation regimes). */
  workspaceReadOnly: boolean;
  coreCompliancePercent: number;
  failedCoreCount: number;
  /** Surveillance: Evaluate step only when OE changed since last audit (mock until API). */
  selectedOeChangedSinceLastAudit: boolean;
}

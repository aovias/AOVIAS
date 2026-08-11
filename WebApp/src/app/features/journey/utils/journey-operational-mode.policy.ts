import { OperationalMode } from '../models/nabh.enums';
import {
  JourneyOperationalModePolicyContext,
  JourneyOperationalModeUiPolicy,
  JourneyProgressBarPresentation,
  WizardEvidenceSectionMode,
  WizardStepInteraction,
  WizardTasksSectionMode,
} from '../models/journey-operational-mode-ui.model';

const WIZARD_STEP_COUNT = 5;

function stepsAll(interaction: WizardStepInteraction): WizardStepInteraction[] {
  return Array.from({ length: WIZARD_STEP_COUNT }, () => interaction);
}

function buildWizardStepsForMode(
  mode: OperationalMode,
  ctx: JourneyOperationalModePolicyContext,
): { steps: WizardStepInteraction[]; blockNav: boolean; allReadOnly: boolean } {
  switch (mode) {
    case OperationalMode.SelfAssessment:
      /** All steps navigable; progression is still gated by `oeWizardCanAccessStep` (ack flags). */
      return {
        steps: stepsAll('active'),
        blockNav: false,
        allReadOnly: false,
      };
    case OperationalMode.InternalAudit:
      return {
        steps: stepsAll('active'),
        blockNav: false,
        allReadOnly: false,
      };
    case OperationalMode.FinalAudit:
      return {
        steps: stepsAll('readonly'),
        blockNav: true,
        allReadOnly: true,
      };
    case OperationalMode.Surveillance: {
      /** Index 3 = Audit Checklist (evaluation); Evidence stays open before audit. */
      const auditMode: WizardStepInteraction =
        ctx.selectedOeChangedSinceLastAudit ? 'active' : 'locked';
      return {
        steps: ['active', 'active', 'active', auditMode, 'active'],
        blockNav: false,
        allReadOnly: false,
      };
    }
    case OperationalMode.ReAccreditation:
      return {
        steps: stepsAll('active'),
        blockNav: false,
        allReadOnly: false,
      };
    default:
      return {
        steps: stepsAll('active'),
        blockNav: false,
        allReadOnly: false,
      };
  }
}

/**
 * Whether switching to `target` is blocked by forward progression rules (not read-only regime).
 */
export function operationalModeProgressionBlocked(
  target: OperationalMode,
  current: OperationalMode,
  ov: { coreCompliancePercent: number; failedCoreCount: number } | null,
): { blocked: boolean; reason: string | null } {
  if (target === current) return { blocked: false, reason: null };
  if (target === OperationalMode.ReAccreditation) {
    return { blocked: true, reason: 'Re-accreditation is not available in this release.' };
  }
  const core = ov?.coreCompliancePercent ?? 0;
  const failed = ov?.failedCoreCount ?? 0;

  if (target < current) return { blocked: false, reason: null };

  if (target >= OperationalMode.InternalAudit && current < OperationalMode.InternalAudit) {
    if (core < 45) {
      return {
        blocked: true,
        reason: 'Reach at least 45% core compliance in Self Assessment before Internal Audit.',
      };
    }
  }
  if (target >= OperationalMode.FinalAudit && current < OperationalMode.FinalAudit) {
    if (core < 65) {
      return {
        blocked: true,
        reason: 'Reach at least 65% core compliance before Final Audit.',
      };
    }
    if (failed > 0) {
      return {
        blocked: true,
        reason: 'Resolve failed core objective elements before Final Audit.',
      };
    }
  }
  if (target >= OperationalMode.Surveillance && current < OperationalMode.Surveillance) {
    if (core < 80) {
      return {
        blocked: true,
        reason: 'Reach at least 80% core compliance before Surveillance.',
      };
    }
    if (failed > 0) {
      return {
        blocked: true,
        reason: 'Resolve failed core objective elements before Surveillance.',
      };
    }
  }
  return { blocked: false, reason: null };
}

function progressPresentation(mode: OperationalMode): JourneyProgressBarPresentation {
  switch (mode) {
    case OperationalMode.SelfAssessment:
      return 'completion';
    case OperationalMode.InternalAudit:
      return 'validation_adjusted';
    case OperationalMode.FinalAudit:
      return 'audit_readiness';
    case OperationalMode.Surveillance:
      return 'delta_vs_last';
    case OperationalMode.ReAccreditation:
      return 'maturity';
    default:
      return 'completion';
  }
}

function progressTitles(mode: OperationalMode): { title: string; subtitle: string } {
  switch (mode) {
    case OperationalMode.SelfAssessment:
      return {
        title: 'NABH Accreditation Progress',
        subtitle: 'Self Assessment — completion',
      };
    case OperationalMode.InternalAudit:
      return {
        title: 'NABH Accreditation Progress',
        subtitle: 'Internal Audit — validation-adjusted score',
      };
    case OperationalMode.FinalAudit:
      return {
        title: 'NABH Accreditation Progress',
        subtitle: 'Final Audit — audit readiness score',
      };
    case OperationalMode.Surveillance:
      return {
        title: 'NABH Accreditation Progress',
        subtitle: 'Surveillance — delta vs last audit',
      };
    case OperationalMode.ReAccreditation:
      return {
        title: 'NABH Accreditation Progress',
        subtitle: 'Re-accreditation — maturity score',
      };
    default:
      return { title: 'NABH Accreditation Progress', subtitle: 'Estimated Audit Readiness' };
  }
}

/**
 * Pure: full UI policy for the active OperationalMode.
 */
export function buildJourneyOperationalModeUiPolicy(
  mode: OperationalMode,
  ctx: JourneyOperationalModePolicyContext,
): JourneyOperationalModeUiPolicy {
  const missingData = false;
  const pt = progressTitles(mode);
  const pres = progressPresentation(mode);
  const wiz = buildWizardStepsForMode(mode, ctx);

  let evidence: WizardEvidenceSectionMode = 'hidden';
  let tasks: WizardTasksSectionMode = 'hidden';
  let evalReq = false;

  switch (mode) {
    case OperationalMode.SelfAssessment:
      evidence = 'hidden';
      tasks = 'hidden';
      break;
    case OperationalMode.InternalAudit:
      evidence = 'required';
      tasks = 'enabled';
      evalReq = true;
      break;
    case OperationalMode.FinalAudit:
      evidence = 'readonly';
      tasks = 'disabled';
      break;
    case OperationalMode.Surveillance:
      evidence = 'conditional';
      tasks = 'enabled';
      break;
    case OperationalMode.ReAccreditation:
      evidence = 'required';
      tasks = 'enabled';
      evalReq = true;
      break;
    default:
      break;
  }

  const showHighRiskOesAction = mode !== OperationalMode.SelfAssessment;

  return {
    mode,
    missingData,
    progressBarPresentation: pres,
    progressCardTitle: pt.title,
    progressCardSubtitle: pt.subtitle,
    showHighRiskOesAction,
    dangerActionEmphasis: mode === OperationalMode.FinalAudit,
    metrics: {
      coreCompliance: true,
      failedCore: mode !== OperationalMode.SelfAssessment,
      lowScore: mode !== OperationalMode.SelfAssessment,
      openActionPlans: mode !== OperationalMode.SelfAssessment,
    },
    execution: {
      chapterShowIssueBadge: mode !== OperationalMode.SelfAssessment,
      chapterHighlightHighRisk: mode === OperationalMode.InternalAudit,
      chapterShowPassFail: mode === OperationalMode.FinalAudit,
      chapterShowChangedSinceAudit: mode === OperationalMode.Surveillance,
      chapterShowMaturityIndicator: mode === OperationalMode.ReAccreditation,
      standardHideOpenIssuesLine: mode === OperationalMode.SelfAssessment,
      standardShowEvidenceGaps: mode === OperationalMode.InternalAudit,
      standardShowComplianceStatus: mode === OperationalMode.FinalAudit,
      standardShowChangeTrend: mode === OperationalMode.Surveillance,
      standardShowPreviouslyFailed: mode === OperationalMode.Surveillance,
      standardShowExcellenceBadge: mode === OperationalMode.ReAccreditation,
      oeHideRiskLabels: mode === OperationalMode.SelfAssessment,
      oeShowClassification: mode !== OperationalMode.SelfAssessment,
      oeRowReadOnly: mode === OperationalMode.FinalAudit,
      oeShowChangedSinceAudit: mode === OperationalMode.Surveillance,
      oeShowExcellenceDetail: mode === OperationalMode.ReAccreditation,
    },
    context: {
      showEvidenceUpload: mode !== OperationalMode.SelfAssessment,
      evidenceUploadDisabled:
        mode === OperationalMode.FinalAudit || ctx.workspaceReadOnly || ctx.snapshotReadOnly,
      evidenceUploadDisabledReason: ctx.workspaceReadOnly
        ? 'Editing is disabled in this audit phase.'
        : ctx.snapshotReadOnly
          ? 'Read-only for this workspace.'
          : mode === OperationalMode.FinalAudit
            ? 'Read-only during Final Audit.'
            : null,
      showTasks: mode !== OperationalMode.SelfAssessment,
      tasksDisabled:
        mode === OperationalMode.FinalAudit ||
        mode === OperationalMode.SelfAssessment ||
        ctx.workspaceReadOnly,
      showKpiInsights: mode === OperationalMode.ReAccreditation,
    },
    wizard: {
      stepInteractions: wiz.steps,
      evidenceSection: evidence,
      tasksSection: tasks,
      evaluateRequiresEvidence: evalReq,
      surveillanceEvaluateOnlyIfChanged: mode === OperationalMode.Surveillance,
      allStepsReadOnly: wiz.allReadOnly,
      blockStepNavigation: wiz.blockNav,
    },
  };
}

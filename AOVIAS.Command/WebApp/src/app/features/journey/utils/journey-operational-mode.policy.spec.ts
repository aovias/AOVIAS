import { OperationalMode } from '../models/nabh.enums';
import {
  buildJourneyOperationalModeUiPolicy,
  operationalModeProgressionBlocked,
} from './journey-operational-mode.policy';
import { JourneyOperationalModePolicyContext } from '../models/journey-operational-mode-ui.model';

const baseCtx = (over?: Partial<JourneyOperationalModePolicyContext>): JourneyOperationalModePolicyContext => ({
  snapshotReadOnly: false,
  workspaceReadOnly: false,
  coreCompliancePercent: 70,
  failedCoreCount: 0,
  selectedOeChangedSinceLastAudit: true,
  ...over,
});

describe('operationalModeProgressionBlocked', () => {
  const ov = { coreCompliancePercent: 70, failedCoreCount: 0 };

  it('allows same mode', () => {
    const r = operationalModeProgressionBlocked(OperationalMode.SelfAssessment, OperationalMode.SelfAssessment, ov);
    expect(r.blocked).toBe(false);
  });

  it('blocks Internal from Self when core too low', () => {
    const r = operationalModeProgressionBlocked(
      OperationalMode.InternalAudit,
      OperationalMode.SelfAssessment,
      { coreCompliancePercent: 30, failedCoreCount: 0 },
    );
    expect(r.blocked).toBe(true);
    expect(r.reason).toContain('45%');
  });

  it('blocks Final when failed core OEs exist', () => {
    const r = operationalModeProgressionBlocked(
      OperationalMode.FinalAudit,
      OperationalMode.InternalAudit,
      { coreCompliancePercent: 70, failedCoreCount: 2 },
    );
    expect(r.blocked).toBe(true);
  });
});

describe('buildJourneyOperationalModeUiPolicy', () => {
  it('hides high-risk action for Self Assessment', () => {
    const p = buildJourneyOperationalModeUiPolicy(OperationalMode.SelfAssessment, baseCtx());
    expect(p.showHighRiskOesAction).toBe(false);
  });

  it('shows high-risk for Internal Audit', () => {
    const p = buildJourneyOperationalModeUiPolicy(OperationalMode.InternalAudit, baseCtx());
    expect(p.showHighRiskOesAction).toBe(true);
  });

  it('locks wizard steps for Final Audit', () => {
    const p = buildJourneyOperationalModeUiPolicy(OperationalMode.FinalAudit, baseCtx());
    expect(p.wizard.allStepsReadOnly).toBe(true);
    expect(p.wizard.blockStepNavigation).toBe(true);
    expect(p.execution.oeRowReadOnly).toBe(true);
  });

  it('hides failed-core metric for Self Assessment', () => {
    const p = buildJourneyOperationalModeUiPolicy(OperationalMode.SelfAssessment, baseCtx());
    expect(p.metrics.failedCore).toBe(false);
    expect(p.metrics.openActionPlans).toBe(false);
  });

  it('shows evidence for Internal Audit', () => {
    const p = buildJourneyOperationalModeUiPolicy(OperationalMode.InternalAudit, baseCtx());
    expect(p.wizard.evidenceSection).toBe('required');
    expect(p.context.showEvidenceUpload).toBe(true);
  });

  it('locks Audit Checklist for Surveillance when OE unchanged', () => {
    const p = buildJourneyOperationalModeUiPolicy(
      OperationalMode.Surveillance,
      baseCtx({ selectedOeChangedSinceLastAudit: false }),
    );
    expect(p.wizard.stepInteractions[3]).toBe('locked');
  });
});

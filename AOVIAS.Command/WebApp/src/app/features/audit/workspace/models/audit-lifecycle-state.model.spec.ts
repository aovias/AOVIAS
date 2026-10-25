import { describe, expect, it } from 'vitest';
import { deriveLifecycleState, type AuditLifecycleFacts } from './audit-lifecycle-state.model';

const statusOf = (facts: AuditLifecycleFacts, stageId: string) =>
  deriveLifecycleState(facts).stages.find((s) => s.stage.id === stageId)?.status;

describe('deriveLifecycleState', () => {
  it('marks nothing complete when no facts are known', () => {
    const state = deriveLifecycleState({});
    expect(state.completedCount).toBe(0);
    expect(state.currentStageId).toBe('planning');
  });

  it('does not claim completion the facts cannot support', () => {
    // The defect this replaces: opening the CAPA tab used to mark Planning,
    // Execution, Evidence and Findings complete regardless of any work.
    const state = deriveLifecycleState({});
    expect(state.stages.every((s) => s.status !== 'complete')).toBe(true);
  });

  it('completes planning once an audit is scheduled', () => {
    expect(statusOf({ hasScheduledAudit: true }, 'planning')).toBe('complete');
  });

  it('treats an unknown count differently from a zero count', () => {
    // Not reported — execution must not be judged complete or incomplete on it.
    const unknown = deriveLifecycleState({ hasScheduledAudit: true, hasSession: true });
    expect(unknown.stages.find((s) => s.stage.id === 'execution')?.outstanding).toEqual([]);

    // Reported as zero — that is a real gap the user should be told about.
    const zero = deriveLifecycleState({
      hasScheduledAudit: true,
      hasSession: true,
      observationCount: 0,
    });
    expect(zero.stages.find((s) => s.stage.id === 'execution')?.outstanding).toContain(
      'Record observations',
    );
  });

  it('completes execution when a session exists with observations', () => {
    const facts = { hasScheduledAudit: true, hasSession: true, observationCount: 3 };
    expect(statusOf(facts, 'execution')).toBe('complete');
  });

  it('reports the earliest incomplete stage as current', () => {
    const state = deriveLifecycleState({
      hasScheduledAudit: true,
      hasSession: true,
      observationCount: 2,
      evidenceCount: 0,
    });
    expect(state.currentStageId).toBe('evidence');
  });

  it('keeps findings open while any finding is unresolved', () => {
    const facts = {
      hasScheduledAudit: true,
      hasSession: true,
      observationCount: 2,
      evidenceCount: 5,
      findingCount: 4,
      openFindingCount: 2,
    };
    expect(statusOf(facts, 'findings')).toBe('current');
    expect(deriveLifecycleState(facts).recommendedAction).toContain('2 finding(s) still open');
  });

  it('settles findings once none remain open', () => {
    const facts = {
      hasScheduledAudit: true,
      hasSession: true,
      observationCount: 2,
      evidenceCount: 5,
      findingCount: 4,
      openFindingCount: 0,
    };
    expect(statusOf(facts, 'findings')).toBe('complete');
  });

  it('recommends the first outstanding item of the current stage', () => {
    const state = deriveLifecycleState({ hasScheduledAudit: false });
    expect(state.recommendedAction).toBe('Schedule an audit');
    expect(state.recommendedStageId).toBe('planning');
  });

  it('explains an unmet prerequisite instead of blocking it', () => {
    const state = deriveLifecycleState({ hasScheduledAudit: true, hasSession: false });
    const evidence = state.stages.find((s) => s.stage.id === 'evidence');

    expect(evidence?.reachable).toBe(false);
    expect(evidence?.blockedReason).toBe('Start the audit before collecting evidence');
  });

  it('guides rather than blocks — every stage still renders', () => {
    const state = deriveLifecycleState({ hasScheduledAudit: false });
    expect(state.stages).toHaveLength(6);
  });

  it('requires closure before the report stage settles', () => {
    const open = deriveLifecycleState({ sessionClosed: false, reportAvailable: false });
    const reports = open.stages.find((s) => s.stage.id === 'reports');
    expect(reports?.outstanding).toContain('Close the audit');
    expect(reports?.blockedReason).toBe('Close the audit to finalise its report');
  });

  it('completes the lifecycle when every stage is satisfied', () => {
    const state = deriveLifecycleState({
      hasScheduledAudit: true,
      hasSession: true,
      sessionClosed: true,
      observationCount: 4,
      evidenceCount: 9,
      findingCount: 3,
      openFindingCount: 0,
      capaCount: 3,
      openCapaCount: 0,
      reportAvailable: true,
    });

    expect(state.completedCount).toBe(6);
    expect(state.recommendedAction).toBeNull();
  });

  it('suggests promoting findings once some exist but no CAPA does', () => {
    const state = deriveLifecycleState({ findingCount: 3, openFindingCount: 1, capaCount: 0 });
    const findings = state.stages.find((s) => s.stage.id === 'findings');

    expect(findings?.recommendations).toContain('Promote accepted findings to CAPA');
    expect(findings?.recommendations).toContain(
      'Triage open findings by severity before promoting them',
    );
  });

  it('changes its advice as the audit progresses', () => {
    // Same stage, different facts — the guidance must not be a static script.
    const early = deriveLifecycleState({ capaCount: 2, openCapaCount: 2 });
    const later = deriveLifecycleState({ capaCount: 2, openCapaCount: 0, verifiedCapaCount: 0 });

    const earlyCapa = early.stages.find((s) => s.stage.id === 'capa');
    const laterCapa = later.stages.find((s) => s.stage.id === 'capa');

    expect(earlyCapa?.recommendations).toContain('Chase owners of CAPAs still awaiting closure');
    expect(laterCapa?.recommendations).toContain(
      'Schedule effectiveness verification for completed actions',
    );
  });

  it('offers no recommendation when the facts support none', () => {
    // Filler advice the user cannot act on trains them to ignore the panel.
    const state = deriveLifecycleState({});
    expect(state.stages.every((s) => s.recommendations.length === 0)).toBe(true);
  });

  it('notes CAPA is raised from a finding when none exist', () => {
    const state = deriveLifecycleState({ findingCount: 0 });
    const capa = state.stages.find((s) => s.stage.id === 'capa');
    expect(capa?.blockedReason).toBe('A CAPA is raised from a finding');
  });
});

/**
 * DEF-RC2W7-001 — the progress bar must describe a state an audit can actually be in.
 *
 * Found in the browser: opening a session that was created independently of a scheduled audit
 * rendered "Planning current · Execution ✓ complete", with Execution simultaneously hinting
 * "Schedule an audit before executing it". Each rule was right; the composition was not.
 */
describe('lifecycle presentation invariants', () => {
  /** The facts that produced the defect: a session with observations but no scheduled audit. */
  const orphanSession: AuditLifecycleFacts = {
    hasScheduledAudit: false,
    hasSession: true,
    observationCount: 2,
  };

  it('never marks a stage complete while an earlier stage is outstanding', () => {
    const { stages } = deriveLifecycleState(orphanSession);
    const firstUnfinished = stages.findIndex((s) => s.status !== 'complete');

    expect(stages[0].status).toBe('current');
    expect(stages.slice(firstUnfinished).every((s) => s.status !== 'complete')).toBe(true);
  });

  it('shows exactly one current stage', () => {
    for (const facts of [{}, orphanSession, { hasScheduledAudit: true, hasSession: true }]) {
      expect(deriveLifecycleState(facts).stages.filter((s) => s.status === 'current')).toHaveLength(
        1,
      );
    }
  });

  it('never shows a blocked hint on a completed stage', () => {
    const facts: AuditLifecycleFacts = {
      hasScheduledAudit: true,
      hasSession: true,
      observationCount: 3,
      evidenceCount: 2,
      sessionClosed: false,
    };

    for (const stage of deriveLifecycleState(facts).stages) {
      if (stage.status === 'complete') {
        expect(stage.blockedReason, stage.stage.id).toBeUndefined();
        expect(stage.reachable, stage.stage.id).toBe(true);
      }
    }
  });

  it('keeps stages after the current one pending, however much later work exists', () => {
    // Findings and CAPAs recorded against a session with no scheduled audit must not light up
    // stages the operator has not legitimately reached.
    const state = deriveLifecycleState({
      ...orphanSession,
      findingCount: 4,
      openFindingCount: 0,
      capaCount: 2,
      openCapaCount: 0,
    });

    const currentIdx = state.stages.findIndex((s) => s.status === 'current');
    expect(state.stages.slice(currentIdx + 1).every((s) => s.status === 'pending')).toBe(true);
  });

  it('still completes every stage once the whole lifecycle is genuinely satisfied', () => {
    // The guard must not prevent a real, in-order completion.
    const state = deriveLifecycleState({
      hasScheduledAudit: true,
      hasSession: true,
      sessionClosed: true,
      observationCount: 4,
      evidenceCount: 9,
      findingCount: 3,
      openFindingCount: 0,
      capaCount: 3,
      openCapaCount: 0,
      reportAvailable: true,
    });

    expect(state.completedCount).toBe(6);
    expect(state.currentStageId).toBeNull();
  });
});

import { describe, expect, it } from 'vitest';
import {
  AUDIT_WORKFLOW_STAGES,
  buildStageProgress,
  getStage,
  nextStage,
  previousStage,
  stageForTab,
  stageIndex,
} from './audit-workflow.model';

describe('audit-workflow.model', () => {
  it('orders the lifecycle planning → execution → evidence → findings → capa → reports', () => {
    expect(AUDIT_WORKFLOW_STAGES.map((s) => s.id)).toEqual([
      'planning',
      'execution',
      'evidence',
      'findings',
      'capa',
      'reports',
    ]);
  });

  it('chains each stage to the next so navigation can be derived, not hand-wired', () => {
    expect(nextStage('planning')?.id).toBe('execution');
    expect(nextStage('findings')?.id).toBe('capa');
  });

  it('reports no next stage at the end of the lifecycle', () => {
    expect(nextStage('reports')).toBeNull();
  });

  it('reports no previous stage at the entry point', () => {
    expect(previousStage('planning')).toBeNull();
    expect(previousStage('evidence')?.id).toBe('execution');
  });

  it('hands each stage what the previous one produces', () => {
    // The handoff contract is what makes the lifecycle continuous rather than a
    // set of independent modules, so a break here is a real workflow defect.
    for (let i = 1; i < AUDIT_WORKFLOW_STAGES.length; i++) {
      const upstream = AUDIT_WORKFLOW_STAGES[i - 1];
      const current = AUDIT_WORKFLOW_STAGES[i];
      const satisfied = current.consumes.some((need) => upstream.produces.includes(need));
      expect(
        satisfied,
        `${current.id} consumes [${current.consumes}] but ${upstream.id} produces [${upstream.produces}]`,
      ).toBe(true);
    }
  });

  it('maps a capability tab to its lifecycle stage', () => {
    expect(stageForTab('capa')?.id).toBe('capa');
    expect(stageForTab('evidence')?.id).toBe('evidence');
  });

  it('places tabs outside the lifecycle in no stage', () => {
    // Overview/intelligence/administration are vantage points over the workflow.
    // Returning a stage would make the progress bar claim a position the user
    // has not actually moved to.
    expect(stageForTab('overview')).toBeNull();
    expect(stageForTab('intelligence')).toBeNull();
    expect(stageForTab('administration')).toBeNull();
  });

  it('marks earlier stages complete, the current stage current, and later ones pending', () => {
    const progress = buildStageProgress('findings');
    expect(progress.map((p) => p.status)).toEqual([
      'complete',
      'complete',
      'complete',
      'current',
      'pending',
      'pending',
    ]);
  });

  it('shows the whole lifecycle as pending when outside it', () => {
    const progress = buildStageProgress(null);
    expect(progress.every((p) => p.status === 'pending')).toBe(true);
  });

  it('resolves stages by id and position', () => {
    expect(getStage('capa').label).toBe('CAPA');
    expect(stageIndex('planning')).toBe(0);
    expect(stageIndex('reports')).toBe(AUDIT_WORKFLOW_STAGES.length - 1);
  });
});

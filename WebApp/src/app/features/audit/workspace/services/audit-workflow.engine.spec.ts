import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';
import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';
import { ConfigurationApiService } from '../../../../core/configuration/configuration-api.service';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import { AuditWorkflowEngine } from './audit-workflow.engine';
import { AUDITOR_ROLE_IDS } from '../models/audit-workspace-roles';
import {
  SEED_AUDIT_WORKFLOW,
  toStageConfigurationRows,
  toSurfaceConfigurationRows,
  type WorkflowConfigurationRow,
} from '../models/workflow-definition.model';
import type { AuditLifecycleFacts } from '../models/audit-lifecycle-state.model';

/**
 * The engine's contract, in two halves.
 *
 * The **seeded** half must reproduce what the hardcoded lifecycle did — `audit-lifecycle-state`'s
 * own spec, written against the old implementation and left untouched, is the primary proof of
 * that; these add the positional and permission behaviour that used to live in a state service and
 * three shell components.
 *
 * The **configured** half is the point of the wave: a lifecycle arriving as data must take effect,
 * and one arriving broken must not.
 */
describe('AuditWorkflowEngine', () => {
  let engine: AuditWorkflowEngine;
  let api: { getCategoryCodes: ReturnType<typeof vi.fn>; getCatalog: ReturnType<typeof vi.fn> };

  /** A configured stage, in the shape the Configuration API delivers. */
  const stageValue = (
    id: string,
    sortOrder: number,
    attributes: Record<string, string>,
    compound: Record<string, unknown>,
  ) => ({ id, label: id, sortOrder, attributes, compound });

  const configure = (category: string, values: unknown[]) => {
    api.getCategoryCodes.mockReturnValue(of([category]));
    api.getCatalog.mockReturnValue(of({ code: category, version: 'v1', values }));
    TestBed.inject(PlatformConfigurationService).load(true);
  };

  beforeEach(() => {
    api = {
      getCategoryCodes: vi.fn(() => of([])),
      getCatalog: vi.fn(() => of(null)),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        AuditWorkflowEngine,
        PlatformConfigurationService,
        { provide: ConfigurationApiService, useValue: api },
      ],
    });
    engine = TestBed.inject(AuditWorkflowEngine);
  });

  describe('platform-default lifecycle', () => {
    it('runs on the seed until configuration says otherwise', () => {
      expect(engine.isConfigured()).toBe(false);
      expect(engine.stages().map((s) => s.id)).toEqual([
        'planning',
        'execution',
        'evidence',
        'findings',
        'capa',
        'reports',
      ]);
    });

    it('chains each stage to the next so navigation is derived, not hand-wired', () => {
      expect(engine.nextStage('planning')?.id).toBe('execution');
      expect(engine.nextStage('reports')).toBeNull();
      expect(engine.previousStage('planning')).toBeNull();
      expect(engine.previousStage('evidence')?.id).toBe('execution');
    });

    it('maps a capability tab to its stage and leaves vantage points outside the lifecycle', () => {
      expect(engine.stageForTab('capa')?.id).toBe('capa');
      // Overview, intelligence and administration look at the workflow rather than sit in it.
      expect(engine.stageForTab('overview')).toBeNull();
      expect(engine.stageForTab('intelligence')).toBeNull();
    });

    it('resolves an unknown stage to nothing rather than throwing', () => {
      // Stage ids are open now, so a typo in a page declaration must degrade, not crash.
      expect(engine.stage('findigns')).toBeNull();
      expect(engine.stageIndex('findigns')).toBe(-1);
      expect(engine.nextStage('findigns')).toBeNull();
    });

    it('reads position as complete-before, current-at, pending-after', () => {
      expect(engine.progressTo('findings').map((p) => p.status)).toEqual([
        'complete',
        'complete',
        'complete',
        'current',
        'pending',
        'pending',
      ]);
      expect(engine.progressTo(null).every((p) => p.status === 'pending')).toBe(true);
    });

    it('hands each stage what the previous one produces', () => {
      const stages = engine.stages();
      for (let i = 1; i < stages.length; i++) {
        const satisfied = stages[i].consumes.some((need) => stages[i - 1].produces.includes(need));
        expect(
          satisfied,
          `${stages[i].id} does not consume what ${stages[i - 1].id} produces`,
        ).toBe(true);
      }
    });
  });

  describe('evaluation', () => {
    it('derives completion, current stage and outstanding work from reported facts', () => {
      const state = engine.evaluate({
        hasScheduledAudit: true,
        hasSession: true,
        observationCount: 2,
        evidenceCount: 0,
      });

      expect(state.currentStageId).toBe('evidence');
      expect(state.completedCount).toBe(2);
      expect(state.recommendedAction).toBe('Collect and verify evidence');
    });

    it('substitutes counts into outstanding messages', () => {
      const state = engine.evaluate({ findingCount: 4, openFindingCount: 2 });
      const findings = state.stages.find((s) => s.stage.id === 'findings');
      expect(findings?.outstanding).toContain('2 finding(s) still open');
    });

    it('explains an unmet prerequisite without making the stage unreachable in the UI', () => {
      const state = engine.evaluate({ hasScheduledAudit: true, hasSession: false });
      const evidence = state.stages.find((s) => s.stage.id === 'evidence');

      expect(evidence?.reachable).toBe(false);
      expect(evidence?.blockedReason).toBe('Start the audit before collecting evidence');
      // Guidance, not enforcement — every stage still renders.
      expect(state.stages).toHaveLength(6);
    });

    it('stays silent about a prerequisite the page never reported on', () => {
      // `hasSession` absent is "not loaded here", not "no session" — the distinction that keeps a
      // page from asserting a blockage it cannot know about.
      const state = engine.evaluate({});
      expect(state.stages.find((s) => s.stage.id === 'evidence')?.blockedReason).toBeUndefined();
    });
  });

  describe('surface permissions', () => {
    it('offers unrestricted surfaces to everyone', () => {
      expect(engine.surfacesFor('overview', []).map((s) => s.id)).toEqual(['programme']);
      expect(engine.surfacesFor('execution', []).map((s) => s.id)).toEqual([
        'surveillance',
        'simulation',
      ]);
    });

    it('adds restricted surfaces for a holder of the required role, in declared order', () => {
      const roles = [AUDITOR_ROLE_IDS[0]];
      expect(engine.surfacesFor('execution', roles).map((s) => s.id)).toEqual([
        'sessions',
        'observations',
        'surveillance',
        'simulation',
      ]);
      expect(engine.surfacesFor('reports', roles).map((s) => s.id)).toEqual([
        'audit',
        'sessions',
        'history',
      ]);
    });

    it('reports the roles a guard should enforce, so route and tab strip cannot disagree', () => {
      expect(engine.rolesFor('execution', 'sessions')).toEqual(AUDITOR_ROLE_IDS);
      // An unrestricted surface returns null — the guard then lets everyone through.
      expect(engine.rolesFor('execution', 'surveillance')).toBeNull();
      expect(engine.rolesFor('execution', 'nope')).toBeNull();
    });
  });

  describe('configured lifecycle', () => {
    it('replaces the lifecycle when configuration supplies one', () => {
      configure(CONFIGURATION_CATEGORY.auditWorkflowStage, [
        stageValue(
          'intake',
          10,
          { tab: 'planning', route: '/app/audit/planning', nextActionLabel: 'Take the request in' },
          {
            completeWhen: { fact: 'hasScheduledAudit', is: 'true' },
            outstanding: [
              {
                when: { not: { fact: 'hasScheduledAudit', is: 'true' } },
                message: 'Log the request',
              },
            ],
          },
        ),
        stageValue(
          'fieldwork',
          20,
          { tab: 'execution', route: '/app/audit/execution', nextActionLabel: 'Go on site' },
          { completeWhen: { fact: 'hasSession', is: 'true' } },
        ),
      ]);

      expect(engine.isConfigured()).toBe(true);
      expect(engine.stages().map((s) => s.id)).toEqual(['intake', 'fieldwork']);
      expect(engine.evaluate({}).recommendedAction).toBe('Log the request');
      expect(engine.evaluate({ hasScheduledAudit: true }).currentStageId).toBe('fieldwork');
    });

    it('honours a configured role restriction on a surface', () => {
      configure(CONFIGURATION_CATEGORY.auditWorkflowSurface, [
        {
          id: 'programme',
          label: 'Programme',
          sortOrder: 10,
          attributes: { tab: 'overview', route: '/app/audit/overview/programme' },
          compound: { requiresAnyRole: ['ORG_ADMIN'] },
        },
      ]);

      expect(engine.surfacesFor('overview', [])).toEqual([]);
      expect(engine.surfacesFor('overview', ['ORG_ADMIN']).map((s) => s.id)).toEqual(['programme']);
    });

    it('rejects a configured lifecycle whose stage has no completion rule', () => {
      // A stage that can never complete would strand the lifecycle on it forever. Falling back to
      // the seed keeps a broken configuration from becoming a broken product.
      configure(CONFIGURATION_CATEGORY.auditWorkflowStage, [
        stageValue('intake', 10, { tab: 'planning', route: '/x' }, {}),
      ]);

      expect(engine.isConfigured()).toBe(false);
      expect(engine.stages().map((s) => s.id)).toContain('planning');
    });

    it('rejects a configured lifecycle carrying a malformed condition', () => {
      configure(CONFIGURATION_CATEGORY.auditWorkflowStage, [
        stageValue(
          'intake',
          10,
          { tab: 'planning', route: '/x' },
          { completeWhen: { fact: 'hasSession', is: 'sometimes' } },
        ),
      ]);

      expect(engine.isConfigured()).toBe(false);
    });

    it('takes the lifecycle whole — a partial set does not overlay the seed', () => {
      configure(CONFIGURATION_CATEGORY.auditWorkflowStage, [
        stageValue(
          'only-stage',
          10,
          { tab: 'planning', route: '/x' },
          { completeWhen: { fact: 'hasScheduledAudit', is: 'true' } },
        ),
      ]);

      // Not "one configured stage plus five platform ones" — that is not a lifecycle.
      expect(engine.stages().map((s) => s.id)).toEqual(['only-stage']);
    });

    it('leaves surfaces on the seed when only stages are configured', () => {
      configure(CONFIGURATION_CATEGORY.auditWorkflowStage, [
        stageValue(
          'intake',
          10,
          { tab: 'planning', route: '/x' },
          { completeWhen: { fact: 'hasScheduledAudit', is: 'true' } },
        ),
      ]);

      expect(engine.surfacesFor('overview', []).map((s) => s.id)).toEqual(['programme']);
    });
  });

  /**
   * The platform defaults ship as configuration rows, which means every organization runs on the
   * *configured* code path rather than the seed. That is only safe if serialising the seed out and
   * reading it back reproduces it — so this asserts the property directly, over the same JSON the
   * seed script writes.
   */
  describe('round trip through configuration', () => {
    /** Serialise → JSON → parse, exactly as the seed script and the API do between them. */
    const asApiValues = (rows: readonly WorkflowConfigurationRow[]) =>
      rows.map((row) => {
        const attributes: Record<string, string | number | boolean> = {};
        const compound: Record<string, unknown> = {};
        for (const [key, v] of Object.entries(JSON.parse(JSON.stringify(row.metadata)))) {
          if (typeof v === 'string' || typeof v === 'number' || typeof v === 'boolean') {
            attributes[key] = v;
          } else if (v !== null && typeof v === 'object') {
            compound[key] = v;
          }
        }
        return { id: row.code, label: row.name, sortOrder: row.sortOrder, attributes, compound };
      });

    const factMatrix: AuditLifecycleFacts[] = [
      {},
      { hasScheduledAudit: false },
      { hasScheduledAudit: true },
      { hasScheduledAudit: true, hasSession: false },
      { hasScheduledAudit: true, hasSession: true, observationCount: 0 },
      { hasScheduledAudit: true, hasSession: true, observationCount: 3, evidenceCount: 0 },
      { evidenceCount: 5, findingCount: 0 },
      { findingCount: 4, openFindingCount: 2 },
      { findingCount: 4, openFindingCount: 0, capaCount: 0 },
      { capaCount: 2, openCapaCount: 2 },
      { capaCount: 2, openCapaCount: 0, verifiedCapaCount: 0 },
      { sessionClosed: false, reportAvailable: false },
      { sessionClosed: true, reportAvailable: false },
      {
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
      },
    ];

    it('reproduces the seeded lifecycle exactly', () => {
      const seeded = factMatrix.map((facts) => engine.evaluate(facts));

      configure(
        CONFIGURATION_CATEGORY.auditWorkflowStage,
        asApiValues(toStageConfigurationRows(SEED_AUDIT_WORKFLOW)),
      );
      expect(engine.isConfigured()).toBe(true);

      factMatrix.forEach((facts, i) => {
        expect(engine.evaluate(facts), `facts #${i}`).toEqual(seeded[i]);
      });
    });

    it('reproduces the seeded surfaces and their role restrictions exactly', () => {
      const roles = [AUDITOR_ROLE_IDS[0]];
      const before = ['overview', 'execution', 'reports'].map((tab) => ({
        open: engine.surfacesFor(tab, []),
        auditor: engine.surfacesFor(tab, roles),
      }));

      configure(
        CONFIGURATION_CATEGORY.auditWorkflowSurface,
        asApiValues(toSurfaceConfigurationRows(SEED_AUDIT_WORKFLOW)),
      );

      ['overview', 'execution', 'reports'].forEach((tab, i) => {
        expect(engine.surfacesFor(tab, []), tab).toEqual(before[i].open);
        expect(engine.surfacesFor(tab, roles), tab).toEqual(before[i].auditor);
      });
      expect(engine.rolesFor('execution', 'sessions')).toEqual(AUDITOR_ROLE_IDS);
    });
  });
});

import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { of } from 'rxjs';
import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';
import { ConfigurationApiService } from '../../../../core/configuration/configuration-api.service';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import { AuditWorkflowEngine } from './audit-workflow.engine';
import {
  SEED_AUDIT_WORKFLOW,
  toStageConfigurationRows,
  toSurfaceConfigurationRows,
} from '../models/workflow-definition.model';
import type { AuditLifecycleFacts } from '../models/audit-lifecycle-state.model';

/**
 * Enterprise configuration simulation — Stage 5C.2, Phases 1 through 4.
 *
 * <p>
 * The claim under test is not "the code compiles" but "an administrator can change what the
 * platform does without anyone touching source". So every case here changes *only* configuration —
 * the same JSON the Configuration API delivers and the seed scripts write — and asserts the change
 * reaches behaviour through the real services.
 * </p>
 *
 * <p>
 * Nothing is stubbed except the HTTP boundary. `ConfigurationApiService` is replaced with a fake
 * transport; `PlatformConfigurationService` and `AuditWorkflowEngine` are the production classes.
 * A test that mocked the store would prove nothing about propagation.
 * </p>
 */
describe('enterprise configuration simulation', () => {
  let configuration: PlatformConfigurationService;
  let workflow: AuditWorkflowEngine;
  let api: { getCategoryCodes: ReturnType<typeof vi.fn>; getCatalog: ReturnType<typeof vi.fn> };

  /** Categories the fake platform currently serves, and their contents. */
  let served: Map<string, { version: string; values: unknown[] }>;

  /** A configuration value as the API delivers it, post-parse. */
  const value = (
    id: string,
    label: string,
    sortOrder: number,
    attributes: Record<string, string | number | boolean> = {},
    compound: Record<string, unknown> = {},
  ) => ({ id, label, sortOrder, attributes, compound });

  /** Publish a category, as an administrator saving an override would. */
  const publish = (code: string, values: unknown[], version = `v${Date.now()}${Math.random()}`) => {
    served.set(code, { version, values });
  };

  /** What a sign-in does: the client asks what exists, then fetches it. */
  const loadSession = () => configuration.load(true);

  beforeEach(() => {
    served = new Map();

    api = {
      getCategoryCodes: vi.fn(() => of([...served.keys()])),
      getCatalog: vi.fn((code: string) => {
        const entry = served.get(code);
        return of(entry ? { code, version: entry.version, values: entry.values } : null);
      }),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        PlatformConfigurationService,
        AuditWorkflowEngine,
        { provide: ConfigurationApiService, useValue: api },
      ],
    });

    configuration = TestBed.inject(PlatformConfigurationService);
    workflow = TestBed.inject(AuditWorkflowEngine);
  });

  // ===============================================================================================
  // Phase 1 — runtime configuration validation
  // ===============================================================================================

  describe('Phase 1 — every configurable category', () => {
    /**
     * Each category, the accessor a screen uses, and a value to rename.
     *
     * Driving them from one table rather than writing twenty near-identical tests means adding a
     * category to the platform and forgetting to validate it is visible as a missing row.
     */
    const categories: {
      code: string;
      probe: string;
      platformLabel: string;
    }[] = [
      { code: CONFIGURATION_CATEGORY.auditType, probe: 'InternalAudit', platformLabel: 'Internal' },
      {
        code: CONFIGURATION_CATEGORY.auditSessionStatus,
        probe: 'InProgress',
        platformLabel: 'In Progress',
      },
      {
        code: CONFIGURATION_CATEGORY.auditSessionResult,
        probe: 'Compliant',
        platformLabel: 'Compliant',
      },
      {
        code: CONFIGURATION_CATEGORY.scheduledAuditStatus,
        probe: 'Completed',
        platformLabel: 'Completed',
      },
      {
        code: CONFIGURATION_CATEGORY.observationType,
        probe: 'MinorObservation',
        platformLabel: 'Minor Observation',
      },
      {
        code: CONFIGURATION_CATEGORY.findingStatus,
        probe: 'InProgress',
        platformLabel: 'In Progress',
      },
      {
        code: CONFIGURATION_CATEGORY.findingSource,
        probe: 'InternalAudit',
        platformLabel: 'Internal Audit',
      },
      {
        code: CONFIGURATION_CATEGORY.findingClassification,
        probe: 'NonConformity',
        platformLabel: 'Non-Conformity',
      },
      {
        code: CONFIGURATION_CATEGORY.findingSeverity,
        probe: 'Critical',
        platformLabel: 'Critical',
      },
      {
        code: CONFIGURATION_CATEGORY.findingAttachmentType,
        probe: 'AuditNote',
        platformLabel: 'Audit Note',
      },
      {
        code: CONFIGURATION_CATEGORY.findingQuickFilter,
        probe: 'needsReview',
        platformLabel: 'Needs Review',
      },
      {
        code: CONFIGURATION_CATEGORY.promotionReadiness,
        probe: 'Eligible',
        platformLabel: 'Eligible',
      },
      {
        code: CONFIGURATION_CATEGORY.deficiencyStatus,
        probe: 'ReviewPending',
        platformLabel: 'Verification Pending',
      },
      { code: CONFIGURATION_CATEGORY.deficiencySeverity, probe: 'Major', platformLabel: 'Major' },
      {
        code: CONFIGURATION_CATEGORY.deficiencyGrouping,
        probe: 'severity',
        platformLabel: 'Group by Severity',
      },
      { code: CONFIGURATION_CATEGORY.riskLevel, probe: 'High', platformLabel: 'High' },
      {
        code: CONFIGURATION_CATEGORY.rootCauseCategory,
        probe: 'PolicyMissing',
        platformLabel: 'Policy Missing',
      },
      {
        code: CONFIGURATION_CATEGORY.capaActionStatus,
        probe: 'NotStarted',
        platformLabel: 'Not Started',
      },
      {
        code: CONFIGURATION_CATEGORY.effectivenessReviewStatus,
        probe: 'NotEffective',
        platformLabel: 'Not Effective',
      },
      {
        code: CONFIGURATION_CATEGORY.effectivenessReviewSchedule,
        probe: '60',
        platformLabel: '60 Day Review',
      },
      { code: CONFIGURATION_CATEGORY.maturityStage, probe: 'Core', platformLabel: 'Core' },
      { code: CONFIGURATION_CATEGORY.department, probe: 'hr', platformLabel: 'HR' },
      { code: CONFIGURATION_CATEGORY.slaStatus, probe: 'AtRisk', platformLabel: 'At Risk' },
      { code: CONFIGURATION_CATEGORY.assignmentConfidence, probe: 'High', platformLabel: 'High' },
      {
        code: CONFIGURATION_CATEGORY.auditRecurrence,
        probe: 'monthly',
        platformLabel: 'Monthly ×3',
      },
      {
        code: CONFIGURATION_CATEGORY.reportPeriod,
        probe: 'quarter',
        platformLabel: 'Last 3 months',
      },
    ];

    it.each(categories)(
      'serves the platform default for $code',
      ({ code, probe, platformLabel }) => {
        expect(configuration.label(code, probe)).toBe(platformLabel);
      },
    );

    it.each(categories)('accepts a tenant override for $code', ({ code, probe }) => {
      publish(code, [value(probe, 'Tenant Wording', 10)]);
      loadSession();

      expect(configuration.label(code, probe)).toBe('Tenant Wording');
    });

    it.each(categories)(
      'restores the platform default for $code when the override is withdrawn',
      ({ code, probe, platformLabel }) => {
        publish(code, [value(probe, 'Tenant Wording', 10)]);
        loadSession();
        expect(configuration.label(code, probe)).toBe('Tenant Wording');

        // Restore Default: the tenant row is removed, so the category resolves to nothing tenant-specific.
        served.delete(code);
        configuration.reset();
        loadSession();

        expect(configuration.label(code, probe)).toBe(platformLabel);
      },
    );

    it('asks only for categories the platform reports, not one request per known catalog', () => {
      publish(CONFIGURATION_CATEGORY.auditType, [value('InternalAudit', 'X', 10)]);
      loadSession();

      expect(api.getCatalog).toHaveBeenCalledTimes(1);
      expect(api.getCatalog).toHaveBeenCalledWith(CONFIGURATION_CATEGORY.auditType);
    });

    it('propagates a version change to bound accessors without a reload', () => {
      const options = configuration.options(CONFIGURATION_CATEGORY.findingSeverity);
      publish(CONFIGURATION_CATEGORY.findingSeverity, [value('Critical', 'Critical', 10)], 'v1');
      loadSession();
      expect(options().map((o) => o.label)).toEqual(['Critical']);

      // An administrator edits and saves; the client refreshes in place.
      publish(
        CONFIGURATION_CATEGORY.findingSeverity,
        [value('Critical', 'Catastrophic', 10), value('Major', 'Serious', 20)],
        'v2',
      );
      configuration.refresh();

      // The same signal instance a component holds now yields the new values.
      expect(options().map((o) => o.label)).toEqual(['Catastrophic', 'Serious']);
    });

    it('does not churn bound signals when the version is unchanged', () => {
      publish(CONFIGURATION_CATEGORY.findingSeverity, [value('Critical', 'Critical', 10)], 'v1');
      loadSession();
      const before = configuration.options(CONFIGURATION_CATEGORY.findingSeverity)();

      configuration.refresh();

      // Identity, not equality: an unchanged catalog must not re-render every consumer.
      expect(configuration.options(CONFIGURATION_CATEGORY.findingSeverity)()).toBe(before);
    });
  });

  // ===============================================================================================
  // Phase 2 — workflow simulation across audit types
  // ===============================================================================================

  describe('Phase 2 — lifecycle simulation', () => {
    /** The eight checkpoints the brief names, expressed as the facts a workspace would report. */
    const journey: {
      checkpoint: string;
      facts: AuditLifecycleFacts;
      expectStage: string | null;
    }[] = [
      { checkpoint: 'Planning', facts: {}, expectStage: 'planning' },
      { checkpoint: 'Execution', facts: { hasScheduledAudit: true }, expectStage: 'execution' },
      {
        checkpoint: 'Evidence',
        facts: { hasScheduledAudit: true, hasSession: true, observationCount: 4 },
        expectStage: 'evidence',
      },
      {
        checkpoint: 'Findings',
        facts: { hasScheduledAudit: true, hasSession: true, observationCount: 4, evidenceCount: 6 },
        expectStage: 'findings',
      },
      {
        checkpoint: 'CAPA',
        facts: {
          hasScheduledAudit: true,
          hasSession: true,
          observationCount: 4,
          evidenceCount: 6,
          findingCount: 3,
          openFindingCount: 0,
        },
        expectStage: 'capa',
      },
      {
        checkpoint: 'Verification',
        facts: {
          hasScheduledAudit: true,
          hasSession: true,
          observationCount: 4,
          evidenceCount: 6,
          findingCount: 3,
          openFindingCount: 0,
          capaCount: 3,
          openCapaCount: 0,
          verifiedCapaCount: 0,
        },
        expectStage: 'reports',
      },
      {
        checkpoint: 'Closure',
        facts: {
          hasScheduledAudit: true,
          hasSession: true,
          observationCount: 4,
          evidenceCount: 6,
          findingCount: 3,
          openFindingCount: 0,
          capaCount: 3,
          openCapaCount: 0,
          sessionClosed: true,
        },
        expectStage: 'reports',
      },
      {
        checkpoint: 'Reports',
        facts: {
          hasScheduledAudit: true,
          hasSession: true,
          observationCount: 4,
          evidenceCount: 6,
          findingCount: 3,
          openFindingCount: 0,
          capaCount: 3,
          openCapaCount: 0,
          sessionClosed: true,
          reportAvailable: true,
        },
        expectStage: null,
      },
    ];

    it.each(journey)('advances to $checkpoint', ({ facts, expectStage }) => {
      expect(workflow.evaluate(facts).currentStageId).toBe(expectStage);
    });

    it('never regresses as work accumulates', () => {
      // Position must be monotonic through the journey: a lifecycle that moved backwards as more
      // work was reported would be worse than none.
      const positions = journey.map((step) => {
        const id = workflow.evaluate(step.facts).currentStageId;
        return id === null ? workflow.stages().length : workflow.stageIndex(id);
      });

      for (let i = 1; i < positions.length; i++) {
        expect(positions[i], journey[i].checkpoint).toBeGreaterThanOrEqual(positions[i - 1]);
      }
    });

    /**
     * The lifecycle is type-agnostic by design, and that is the finding worth recording: the six
     * audit types differ in scheduling and surveillance attributes, not in the stages they pass
     * through. Asserting it stops a later change from quietly making the lifecycle type-dependent
     * in code rather than in configuration.
     */
    it.each([
      'InternalAudit',
      'DepartmentAudit',
      'MockAudit',
      'FocusedAudit',
      'Surveillance',
      'FollowUpAudit',
    ])('runs the same configured lifecycle for %s', (auditType) => {
      expect(configuration.descriptor(CONFIGURATION_CATEGORY.auditType, auditType)).not.toBeNull();
      expect(workflow.stages().map((s) => s.id)).toEqual([
        'planning',
        'execution',
        'evidence',
        'findings',
        'capa',
        'reports',
      ]);
    });

    it('offers each type where its attributes say it belongs', () => {
      const schedulable = configuration
        .valuesWith(CONFIGURATION_CATEGORY.auditType, 'selectableWhenScheduling')()
        .map((t) => t.id);
      const startable = configuration
        .valuesWith(CONFIGURATION_CATEGORY.auditType, 'selectableWhenStartingSession')()
        .map((t) => t.id);
      const surveillance = configuration
        .valuesWith(CONFIGURATION_CATEGORY.auditType, 'surveillance')()
        .map((t) => t.id);

      expect(schedulable).toContain('DepartmentAudit');
      expect(startable).toContain('ConsultantReview');
      expect(surveillance).toEqual(['Surveillance']);
    });
  });

  // ===============================================================================================
  // Phase 3 — tenant isolation
  // ===============================================================================================

  describe('Phase 3 — tenant isolation', () => {
    const acme = () => {
      publish(
        CONFIGURATION_CATEGORY.auditType,
        [value('InternalAudit', 'Acme Review', 10)],
        'shared',
      );
      publish(
        CONFIGURATION_CATEGORY.auditWorkflowStage,
        [
          value(
            'intake',
            'Intake',
            10,
            { tab: 'planning', route: '/x' },
            {
              completeWhen: { fact: 'hasScheduledAudit', is: 'true' },
            },
          ),
        ],
        'shared',
      );
    };

    const beta = () => {
      publish(
        CONFIGURATION_CATEGORY.auditType,
        [value('InternalAudit', 'Beta Audit', 10)],
        'shared',
      );
      served.delete(CONFIGURATION_CATEGORY.auditWorkflowStage);
    };

    /** What signing out of one organization and into another does. */
    const switchTenant = (next: () => void) => {
      configuration.reset();
      next();
      loadSession();
    };

    it('carries no labels from one organization to the next', () => {
      acme();
      loadSession();
      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Acme Review');

      switchTenant(beta);
      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Beta Audit');
    });

    it('carries no workflow from one organization to the next', () => {
      acme();
      loadSession();
      expect(workflow.stages().map((s) => s.id)).toEqual(['intake']);

      // Beta has configured no lifecycle, so it must get the platform default — not Acme's.
      switchTenant(beta);
      expect(workflow.stages().map((s) => s.id)).toEqual([
        'planning',
        'execution',
        'evidence',
        'findings',
        'capa',
        'reports',
      ]);
    });

    it('is not fooled by two organizations whose versions collide', () => {
      // Versions are per-tenant hashes and can coincide. A version check that survived the reset
      // would treat the incoming tenant's catalog as unchanged and keep the outgoing tenant's.
      acme();
      loadSession();
      switchTenant(beta);

      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Beta Audit');
    });

    it('restores platform defaults between organizations, not the previous tenant’s values', () => {
      acme();
      loadSession();
      configuration.reset();

      // The window between sign-out and the next sign-in must show platform behaviour.
      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Internal');
      expect(workflow.isConfigured()).toBe(false);
    });
  });

  // ===============================================================================================
  // Phase 4 — business scenario simulation
  // ===============================================================================================

  describe('Phase 4 — administration scenarios, configuration only', () => {
    it('renames an audit type', () => {
      publish(CONFIGURATION_CATEGORY.auditType, [
        value('InternalAudit', 'First-Party Review', 10, { selectableWhenScheduling: true }),
      ]);
      loadSession();

      expect(configuration.auditTypeLabel('InternalAudit')).toBe('First-Party Review');
      expect(configuration.schedulableAuditTypes().map((t) => t.label)).toEqual([
        'First-Party Review',
      ]);
    });

    it('disables an audit type by withdrawing it from scheduling', () => {
      publish(CONFIGURATION_CATEGORY.auditType, [
        value('InternalAudit', 'Internal', 10, { selectableWhenScheduling: false }),
        value('MockAudit', 'Mock NABH', 20, { selectableWhenScheduling: true }),
      ]);
      loadSession();

      expect(configuration.schedulableAuditTypes().map((t) => t.id)).toEqual(['MockAudit']);
      // Still resolvable for display — historical records referencing it must not blank out.
      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Internal');
    });

    it('reorders the workflow', () => {
      const rows = toStageConfigurationRows(SEED_AUDIT_WORKFLOW).map((r) =>
        // Evidence before Execution.
        r.code === 'evidence' ? { ...r, sortOrder: 15 } : r,
      );
      publish(
        CONFIGURATION_CATEGORY.auditWorkflowStage,
        rows.map((r) => toValue(r)),
      );
      loadSession();

      expect(workflow.stages().map((s) => s.id)).toEqual([
        'planning',
        'evidence',
        'execution',
        'findings',
        'capa',
        'reports',
      ]);
      expect(workflow.nextStage('planning')?.id).toBe('evidence');
    });

    it('adds a workflow stage', () => {
      const rows = toStageConfigurationRows(SEED_AUDIT_WORKFLOW).map(toValue);
      rows.push(
        value(
          'verification',
          'Verification',
          55,
          { tab: 'capa', route: '/app/audit/capa', nextActionLabel: 'Verify effectiveness' },
          {
            completeWhen: { fact: 'verifiedCapaCount', is: 'positive' },
            outstanding: [
              {
                when: { not: { fact: 'verifiedCapaCount', is: 'positive' } },
                message: 'Verify CAPA effectiveness',
              },
            ],
          },
        ),
      );
      publish(CONFIGURATION_CATEGORY.auditWorkflowStage, rows);
      loadSession();

      expect(workflow.stages().map((s) => s.id)).toEqual([
        'planning',
        'execution',
        'evidence',
        'findings',
        'capa',
        'verification',
        'reports',
      ]);

      const state = workflow.evaluate({
        hasScheduledAudit: true,
        hasSession: true,
        observationCount: 2,
        evidenceCount: 3,
        findingCount: 1,
        openFindingCount: 0,
        capaCount: 1,
        openCapaCount: 0,
      });
      expect(state.currentStageId).toBe('verification');
      expect(state.recommendedAction).toBe('Verify CAPA effectiveness');
    });

    it('restricts a stage permission', () => {
      const rows = toSurfaceConfigurationRows(SEED_AUDIT_WORKFLOW).map((r) =>
        r.code === 'execution.surveillance'
          ? { ...r, metadata: { ...r.metadata, requiresAnyRole: ['ORG_ADMIN'] } }
          : r,
      );
      publish(CONFIGURATION_CATEGORY.auditWorkflowSurface, rows.map(toValue));
      loadSession();

      expect(workflow.surfacesFor('execution', []).map((s) => s.id)).toEqual(['simulation']);
      expect(workflow.surfacesFor('execution', ['ORG_ADMIN']).map((s) => s.id)).toContain(
        'surveillance',
      );
      // The guard reads the same declaration, so the tab strip cannot offer what the route rejects.
      expect(workflow.rolesFor('execution', 'surveillance')).toEqual(['ORG_ADMIN']);
    });

    it('hides a status from a filter without deleting it', () => {
      publish(CONFIGURATION_CATEGORY.findingStatus, [
        value('Draft', 'Draft', 10, { selectableWhenFiltering: true }),
        value('Closed', 'Closed', 20, { selectableWhenFiltering: false }),
      ]);
      loadSession();

      const filterable = configuration
        .optionsWith(CONFIGURATION_CATEGORY.findingStatus, 'selectableWhenFiltering')()
        .map((o) => o.value);

      expect(filterable).toEqual(['Draft']);
      // Hidden from the picker, still rendered on records that hold it.
      expect(configuration.label(CONFIGURATION_CATEGORY.findingStatus, 'Closed')).toBe('Closed');
    });

    it('reveals a status that the platform default hides', () => {
      // The two statuses that were previously unfilterable by omission — now a toggle.
      publish(CONFIGURATION_CATEGORY.findingStatus, [
        value('InProgress', 'In Progress', 10, { selectableWhenFiltering: true }),
        value('Verified', 'Verified', 20, { selectableWhenFiltering: true }),
      ]);
      loadSession();

      expect(
        configuration
          .optionsWith(CONFIGURATION_CATEGORY.findingStatus, 'selectableWhenFiltering')()
          .map((o) => o.value),
      ).toEqual(['InProgress', 'Verified']);
    });

    it('changes display labels across every surface that reads them', () => {
      publish(CONFIGURATION_CATEGORY.deficiencyStatus, [
        value('ReviewPending', 'Awaiting Sign-off', 10, {
          lifecycleStep: true,
          selectableWhenFiltering: true,
        }),
      ]);
      loadSession();

      // One edit; grid, detail panel, dashboard breakdown and filter all read this accessor.
      expect(configuration.label(CONFIGURATION_CATEGORY.deficiencyStatus, 'ReviewPending')).toBe(
        'Awaiting Sign-off',
      );
      expect(
        configuration.optionsWith(
          CONFIGURATION_CATEGORY.deficiencyStatus,
          'selectableWhenFiltering',
        )(),
      ).toEqual([{ value: 'ReviewPending', label: 'Awaiting Sign-off' }]);
    });

    it('modifies a transition rule', () => {
      // Findings may no longer be reopened to Draft; only Reviewed and Closed are offered.
      publish(CONFIGURATION_CATEGORY.findingStatus, [
        value('Draft', 'Draft', 10, { selectableWhenTransitioning: false }),
        value('Reviewed', 'Reviewed', 20, { selectableWhenTransitioning: true }),
        value('Closed', 'Closed', 30, { selectableWhenTransitioning: true }),
      ]);
      loadSession();

      expect(
        configuration
          .optionsWith(CONFIGURATION_CATEGORY.findingStatus, 'selectableWhenTransitioning')()
          .map((o) => o.value),
      ).toEqual(['Reviewed', 'Closed']);
    });

    it('modifies a completion rule', () => {
      // Evidence now requires findings to have been raised as well as evidence collected.
      const rows = toStageConfigurationRows(SEED_AUDIT_WORKFLOW).map((r) =>
        r.code === 'evidence'
          ? {
              ...r,
              metadata: {
                ...r.metadata,
                completeWhen: {
                  all: [
                    { fact: 'evidenceCount', is: 'positive' },
                    { fact: 'findingCount', is: 'positive' },
                  ],
                },
              },
            }
          : r,
      );
      publish(CONFIGURATION_CATEGORY.auditWorkflowStage, rows.map(toValue));
      loadSession();

      const facts = {
        hasScheduledAudit: true,
        hasSession: true,
        observationCount: 2,
        evidenceCount: 5,
      };
      // Under platform rules this would have advanced to findings.
      expect(workflow.evaluate(facts).currentStageId).toBe('evidence');
      expect(
        workflow.evaluate({ ...facts, findingCount: 1, openFindingCount: 1 }).currentStageId,
      ).toBe('findings');
    });

    it('restores defaults', () => {
      publish(CONFIGURATION_CATEGORY.auditType, [value('InternalAudit', 'Renamed', 10)]);
      publish(
        CONFIGURATION_CATEGORY.auditWorkflowStage,
        toStageConfigurationRows(SEED_AUDIT_WORKFLOW)
          .filter((r) => r.code === 'planning')
          .map(toValue),
      );
      loadSession();
      expect(workflow.stages()).toHaveLength(1);

      // Restore Default removes the tenant rows; the next resolve falls through to the platform.
      served.clear();
      configuration.reset();
      loadSession();

      expect(configuration.auditTypeLabel('InternalAudit')).toBe('Internal');
      expect(workflow.stages()).toHaveLength(6);
      expect(workflow.isConfigured()).toBe(false);
    });

    it('moves the risk-matrix bands', () => {
      // DEF-CFG-003. An organization on a 3×3 matrix needs different boundaries, not just different
      // names; the CAPA panel held 20/12/6 as an `if` ladder.
      publish(CONFIGURATION_CATEGORY.riskLevel, [
        value('Low', 'Low', 10, { minScore: 0 }),
        value('Medium', 'Medium', 20, { minScore: 3 }),
        value('High', 'High', 30, { minScore: 6 }),
      ]);
      loadSession();

      const band = (score: number) =>
        [...configuration.values(CONFIGURATION_CATEGORY.riskLevel)()]
          .filter((l) => typeof l.attributes['minScore'] === 'number')
          .sort((a, b) => Number(b.attributes['minScore']) - Number(a.attributes['minScore']))
          .find((l) => score >= Number(l.attributes['minScore']))?.label;

      expect(band(9)).toBe('High');
      expect(band(4)).toBe('Medium');
      expect(band(1)).toBe('Low');
    });

    it('moves which status permits owner acceptance', () => {
      // DEF-CFG-002. The CAPA panel tested `status === 'Assigned'`, so renaming the status removed
      // the accept/reject controls.
      publish(CONFIGURATION_CATEGORY.deficiencyStatus, [
        value('Assigned', 'Assigned', 10, { awaitingOwnerAcceptance: false }),
        value('Dispatched', 'Dispatched', 20, { awaitingOwnerAcceptance: true }),
      ]);
      loadSession();

      const awaiting = (status: string) =>
        configuration.attribute(
          CONFIGURATION_CATEGORY.deficiencyStatus,
          status,
          'awaitingOwnerAcceptance',
          false,
        );

      expect(awaiting('Dispatched')).toBe(true);
      expect(awaiting('Assigned')).toBe(false);
    });

    it('refuses a lifecycle that would strand the workspace, and says so by staying on the default', () => {
      publish(CONFIGURATION_CATEGORY.auditWorkflowStage, [
        value(
          'broken',
          'Broken',
          10,
          { tab: 'planning', route: '/x' },
          {
            completeWhen: { fact: 'hasSession', is: 'whenever' },
          },
        ),
      ]);
      loadSession();

      expect(workflow.isConfigured()).toBe(false);
      expect(workflow.stages()).toHaveLength(6);
    });
  });
});

/** A serialised configuration row as the API delivers it after metadata parsing. */
function toValue(row: {
  code: string;
  name: string;
  sortOrder: number;
  metadata: Record<string, unknown>;
}) {
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
}

import { AUDITOR_ROLE_IDS } from './audit-workspace-roles';
import type { WorkflowCondition } from './workflow-condition.model';

/**
 * The Audit lifecycle, as a declarative definition.
 *
 * <p>
 * **What changed.** The lifecycle used to be an ordered array of stage *descriptions* plus four
 * `switch` statements holding its *rules* — completion, outstanding work, entry prerequisites and
 * recommendations — spread across two files, with permissions duplicated a third time inside three
 * shell components. A stage's behaviour could only be understood by reading all of them.
 * </p>
 *
 * <p>
 * Here a stage carries its own rules. Every rule is a {@link WorkflowCondition} over reported facts,
 * which means it is JSON, which means it can come from the Configuration Engine instead of from
 * this file. {@link SEED_AUDIT_WORKFLOW} reproduces the previous behaviour exactly — same messages,
 * same order, same edge cases — so this wave changes the source of truth and nothing else.
 * </p>
 *
 * <p>
 * **Stage order is the array order.** `order` exists so a configured definition can reorder stages
 * without depending on serialisation order, but nothing derives meaning from the *value* beyond
 * sorting — the previous code used array position for progress, next-stage and previous-stage, and
 * that is preserved.
 * </p>
 */

/** A rule that contributes a message when its condition holds. */
export interface WorkflowMessageRule {
  readonly when: WorkflowCondition;
  /** Supports `{factName}` placeholders, substituted from the reported facts. */
  readonly message: string;
}

/**
 * A sub-navigation surface within a capability tab.
 *
 * Modelled separately from stages because the surfaces that carry role restrictions do not map
 * one-to-one onto lifecycle stages: Overview has two surfaces but is not a lifecycle stage at all,
 * while Planning is a stage with no surfaces. Three shell components each held their own copy of
 * this list together with an identical role check.
 */
export interface WorkflowSurfaceDefinition {
  readonly id: string;
  /** Capability tab that owns this surface. */
  readonly tab: string;
  readonly label: string;
  readonly route: string;
  readonly order: number;
  /**
   * Roles that may see and reach this surface. Absent means everyone.
   *
   * The same set guards the route, so the workspace cannot advertise a destination the guard would
   * reject — the property the original consolidation was careful to preserve.
   */
  readonly requiresAnyRole?: readonly string[];
}

/** One lifecycle stage and every rule that governs it. */
export interface WorkflowStageDefinition {
  readonly id: string;
  readonly label: string;
  /** Capability tab that hosts this stage — how a stage resolves to a route. */
  readonly tab: string;
  /** Canonical route for entering the stage without a specific record. */
  readonly route: string;
  /** Position in the lifecycle. Lower first. */
  readonly order: number;
  /** What the previous stage hands over. Empty for the entry stage. */
  readonly consumes: readonly string[];
  /** What completing this stage yields for the next one. */
  readonly produces: readonly string[];
  /** Sentence shown when the workspace suggests this as the next activity. */
  readonly nextActionLabel: string;

  /** When this stage counts as done. */
  readonly completeWhen: WorkflowCondition;
  /** What still has to happen here, in the order it should be read. */
  readonly outstanding: readonly WorkflowMessageRule[];
  /** What is worth doing given the facts — advice, distinct from what is missing. */
  readonly recommendations: readonly WorkflowMessageRule[];
  /**
   * Why the stage cannot yet be entered, when it cannot.
   *
   * Guidance, never enforcement. The status bar dims the stage and explains; it does not prevent
   * navigation, because an auditor may legitimately need to look ahead.
   */
  readonly entryBlockedWhen?: WorkflowMessageRule;
}

/** The whole lifecycle: its stages and the surfaces inside its capability tabs. */
export interface WorkflowDefinition {
  readonly stages: readonly WorkflowStageDefinition[];
  readonly surfaces: readonly WorkflowSurfaceDefinition[];
}

const AUDIT_WORKSPACE_BASE = '/app/audit';

/**
 * Platform-default lifecycle.
 *
 * A seed, not a fallback branch: it is the starting content of the same definition the engine
 * serves, so the configured and unconfigured paths cannot behave differently.
 *
 * Each rule below is a transcription of the code it replaces. The transcriptions worth explaining:
 *
 * - `is: 'false'` on every `entryBlockedWhen` is deliberate and is **not** the same as
 *   `not: { is: 'true' }`. The originals tested `f.hasSession === false`, so a page that never
 *   loaded the fact reported no blockage rather than asserting one it could not know about.
 * - The `outstanding` chains that were `if / else if` become explicitly mutually exclusive
 *   conditions. Same output, but the second item's dependency on the first is now stated rather
 *   than implied by control flow.
 * - Reports is the one stage whose outstanding items were *not* an else-chain: an unclosed audit
 *   with no report yields both messages, and that is preserved.
 */
export const SEED_AUDIT_WORKFLOW: WorkflowDefinition = {
  stages: [
    {
      id: 'planning',
      label: 'Planning',
      tab: 'planning',
      route: `${AUDIT_WORKSPACE_BASE}/planning`,
      order: 10,
      consumes: [],
      produces: ['scheduledAudit'],
      nextActionLabel: 'Schedule an audit',
      completeWhen: { fact: 'hasScheduledAudit', is: 'true' },
      outstanding: [
        { when: { not: { fact: 'hasScheduledAudit', is: 'true' } }, message: 'Schedule an audit' },
      ],
      recommendations: [
        {
          when: {
            all: [
              { fact: 'hasScheduledAudit', is: 'true' },
              { not: { fact: 'hasSession', is: 'true' } },
            ],
          },
          message: 'Assign the audit team before execution begins',
        },
      ],
    },
    {
      id: 'execution',
      label: 'Execution',
      tab: 'execution',
      route: `${AUDIT_WORKSPACE_BASE}/execution/sessions`,
      order: 20,
      consumes: ['scheduledAudit'],
      produces: ['auditSession', 'observation'],
      nextActionLabel: 'Start the audit',
      completeWhen: {
        all: [
          { fact: 'hasSession', is: 'true' },
          { fact: 'observationCount', is: 'positive' },
        ],
      },
      outstanding: [
        { when: { not: { fact: 'hasSession', is: 'true' } }, message: 'Start the audit session' },
        {
          when: {
            all: [
              { fact: 'hasSession', is: 'true' },
              { fact: 'observationCount', is: 'zero' },
            ],
          },
          message: 'Record observations',
        },
      ],
      recommendations: [
        {
          when: {
            all: [
              { fact: 'hasSession', is: 'true' },
              { fact: 'observationCount', is: 'positive' },
              { not: { fact: 'evidenceCount', is: 'positive' } },
            ],
          },
          message: 'Attach evidence to the observations recorded so far',
        },
      ],
      entryBlockedWhen: {
        when: { fact: 'hasScheduledAudit', is: 'false' },
        message: 'Schedule an audit before executing it',
      },
    },
    {
      id: 'evidence',
      label: 'Evidence',
      tab: 'evidence',
      route: `${AUDIT_WORKSPACE_BASE}/evidence`,
      order: 30,
      consumes: ['auditSession'],
      produces: ['evidence'],
      nextActionLabel: 'Collect and review evidence',
      completeWhen: { fact: 'evidenceCount', is: 'positive' },
      outstanding: [
        {
          when: { not: { fact: 'evidenceCount', is: 'positive' } },
          message: 'Collect and verify evidence',
        },
      ],
      recommendations: [
        {
          when: {
            all: [
              { fact: 'evidenceCount', is: 'positive' },
              { fact: 'findingCount', is: 'zero' },
            ],
          },
          message: 'Review collected evidence for issues worth raising as findings',
        },
      ],
      entryBlockedWhen: {
        when: { fact: 'hasSession', is: 'false' },
        message: 'Start the audit before collecting evidence',
      },
    },
    {
      id: 'findings',
      label: 'Findings',
      tab: 'findings',
      route: `${AUDIT_WORKSPACE_BASE}/findings`,
      order: 40,
      consumes: ['observation', 'evidence'],
      produces: ['finding'],
      nextActionLabel: 'Raise findings from observations',
      completeWhen: {
        all: [
          { fact: 'findingCount', is: 'positive' },
          { fact: 'openFindingCount', is: 'zero' },
        ],
      },
      outstanding: [
        {
          when: { not: { fact: 'findingCount', is: 'positive' } },
          message: 'Raise findings from observations',
        },
        {
          when: {
            all: [
              { fact: 'findingCount', is: 'positive' },
              { fact: 'openFindingCount', is: 'positive' },
            ],
          },
          message: '{openFindingCount} finding(s) still open',
        },
      ],
      recommendations: [
        {
          when: { fact: 'openFindingCount', is: 'positive' },
          message: 'Triage open findings by severity before promoting them',
        },
        {
          when: {
            all: [
              { fact: 'findingCount', is: 'positive' },
              { fact: 'capaCount', is: 'zero' },
            ],
          },
          message: 'Promote accepted findings to CAPA',
        },
      ],
      entryBlockedWhen: {
        when: { fact: 'hasSession', is: 'false' },
        message: 'Findings are raised during an audit session',
      },
    },
    {
      id: 'capa',
      label: 'CAPA',
      tab: 'capa',
      route: `${AUDIT_WORKSPACE_BASE}/capa`,
      order: 50,
      consumes: ['finding'],
      produces: ['correctiveAction'],
      nextActionLabel: 'Promote findings to CAPA',
      completeWhen: {
        all: [
          { fact: 'capaCount', is: 'positive' },
          { fact: 'openCapaCount', is: 'zero' },
        ],
      },
      outstanding: [
        {
          when: { not: { fact: 'capaCount', is: 'positive' } },
          message: 'Promote findings to CAPA',
        },
        {
          when: {
            all: [
              { fact: 'capaCount', is: 'positive' },
              { fact: 'openCapaCount', is: 'positive' },
            ],
          },
          message: '{openCapaCount} CAPA(s) awaiting closure',
        },
      ],
      recommendations: [
        {
          when: { fact: 'openCapaCount', is: 'positive' },
          message: 'Chase owners of CAPAs still awaiting closure',
        },
        {
          when: {
            all: [
              { fact: 'capaCount', is: 'positive' },
              { fact: 'verifiedCapaCount', is: 'zero' },
            ],
          },
          message: 'Schedule effectiveness verification for completed actions',
        },
      ],
      entryBlockedWhen: {
        when: { fact: 'findingCount', is: 'zero' },
        message: 'A CAPA is raised from a finding',
      },
    },
    {
      id: 'reports',
      label: 'Reports',
      tab: 'reports',
      route: `${AUDIT_WORKSPACE_BASE}/reports`,
      order: 60,
      consumes: ['correctiveAction'],
      produces: ['auditReport'],
      nextActionLabel: 'Generate the audit report',
      completeWhen: { fact: 'reportAvailable', is: 'true' },
      outstanding: [
        { when: { not: { fact: 'sessionClosed', is: 'true' } }, message: 'Close the audit' },
        {
          when: { not: { fact: 'reportAvailable', is: 'true' } },
          message: 'Generate the audit report',
        },
      ],
      recommendations: [
        {
          when: {
            all: [
              { fact: 'sessionClosed', is: 'true' },
              { not: { fact: 'reportAvailable', is: 'true' } },
            ],
          },
          message: 'Generate the audit report and executive summary',
        },
      ],
      entryBlockedWhen: {
        when: { fact: 'sessionClosed', is: 'false' },
        message: 'Close the audit to finalise its report',
      },
    },
  ],

  surfaces: [
    {
      id: 'programme',
      tab: 'overview',
      label: 'Programme',
      route: `${AUDIT_WORKSPACE_BASE}/overview/programme`,
      order: 10,
    },
    {
      id: 'auditor-home',
      tab: 'overview',
      label: 'Auditor Home',
      route: `${AUDIT_WORKSPACE_BASE}/overview/auditor-home`,
      order: 20,
      requiresAnyRole: AUDITOR_ROLE_IDS,
    },

    {
      id: 'sessions',
      tab: 'execution',
      label: 'Active Audits',
      route: `${AUDIT_WORKSPACE_BASE}/execution/sessions`,
      order: 10,
      requiresAnyRole: AUDITOR_ROLE_IDS,
    },
    {
      id: 'observations',
      tab: 'execution',
      label: 'Observations',
      route: `${AUDIT_WORKSPACE_BASE}/execution/observations`,
      order: 20,
      requiresAnyRole: AUDITOR_ROLE_IDS,
    },
    {
      id: 'surveillance',
      tab: 'execution',
      label: 'Surveillance',
      route: `${AUDIT_WORKSPACE_BASE}/execution/surveillance`,
      order: 30,
    },
    {
      id: 'simulation',
      tab: 'execution',
      label: 'Mock / Simulation',
      route: `${AUDIT_WORKSPACE_BASE}/execution/simulation`,
      order: 40,
    },

    {
      id: 'audit',
      tab: 'reports',
      label: 'Audit Reports',
      route: `${AUDIT_WORKSPACE_BASE}/reports/audit`,
      order: 10,
    },
    {
      id: 'sessions',
      tab: 'reports',
      label: 'Session Reports',
      route: `${AUDIT_WORKSPACE_BASE}/reports/sessions`,
      order: 20,
      requiresAnyRole: AUDITOR_ROLE_IDS,
    },
    {
      id: 'history',
      tab: 'reports',
      label: 'History',
      route: `${AUDIT_WORKSPACE_BASE}/reports/history`,
      order: 30,
      requiresAnyRole: AUDITOR_ROLE_IDS,
    },
  ],
};

/** A stage or surface as the Configuration API stores it: scalars, plus a JSON metadata document. */
export interface WorkflowConfigurationRow {
  readonly code: string;
  readonly name: string;
  readonly sortOrder: number;
  /** Contents of the `Metadata` column. */
  readonly metadata: Record<string, unknown>;
}

/**
 * Serialise a lifecycle into configuration rows — the inverse of {@link toStages}.
 *
 * <p>
 * Two jobs. It gives an administrator a starting point rather than a blank category to hand-write
 * JSON into, and it makes the mapper's correctness *testable*: feeding the seed through this and
 * back must reproduce the seed's behaviour exactly, which is what justifies shipping the platform
 * defaults as configuration rows at all.
 * </p>
 */
export function toStageConfigurationRows(
  definition: WorkflowDefinition,
): readonly WorkflowConfigurationRow[] {
  return definition.stages.map((stage) => ({
    code: stage.id,
    name: stage.label,
    sortOrder: stage.order,
    metadata: {
      tab: stage.tab,
      route: stage.route,
      nextActionLabel: stage.nextActionLabel,
      consumes: stage.consumes,
      produces: stage.produces,
      completeWhen: stage.completeWhen,
      outstanding: stage.outstanding,
      recommendations: stage.recommendations,
      ...(stage.entryBlockedWhen ? { entryBlockedWhen: stage.entryBlockedWhen } : {}),
    },
  }));
}

export function toSurfaceConfigurationRows(
  definition: WorkflowDefinition,
): readonly WorkflowConfigurationRow[] {
  return definition.surfaces.map((surface) => ({
    // Surface ids repeat across tabs (`sessions` exists under both execution and reports), so the
    // configuration code has to carry the tab to stay unique within its category.
    code: `${surface.tab}.${surface.id}`,
    name: surface.label,
    sortOrder: surface.order,
    metadata: {
      tab: surface.tab,
      surfaceId: surface.id,
      route: surface.route,
      ...(surface.requiresAnyRole ? { requiresAnyRole: surface.requiresAnyRole } : {}),
    },
  }));
}

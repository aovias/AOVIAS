import { Injectable, computed, inject } from '@angular/core';
import { CONFIGURATION_CATEGORY } from '../../../../core/configuration/audit-configuration.catalogs';
import type { ConfigurationValueDescriptor } from '../../../../core/configuration/configuration-catalog';
import { PlatformConfigurationService } from '../../../../core/configuration/platform-configuration.service';
import {
  deriveLifecycleState,
  type AuditLifecycleFacts,
  type AuditLifecycleState,
} from '../models/audit-lifecycle-state.model';
import type {
  AuditWorkflowStage,
  AuditWorkflowStageId,
  AuditWorkflowStageProgress,
} from '../models/audit-workflow.model';
import { parseWorkflowCondition, type WorkflowCondition } from '../models/workflow-condition.model';
import {
  SEED_AUDIT_WORKFLOW,
  type WorkflowDefinition,
  type WorkflowMessageRule,
  type WorkflowStageDefinition,
  type WorkflowSurfaceDefinition,
} from '../models/workflow-definition.model';

/**
 * The audit lifecycle engine.
 *
 * <p>
 * **The one place workflow decisions are made.** Before this, "which stage am I in", "what comes
 * next", "is this stage done", "why is it blocked" and "may this user see this surface" were
 * answered independently by a state service, a status bar, three shell components and four `switch`
 * statements. Consumers now ask; they never decide.
 * </p>
 *
 * <p>
 * **Configuration-driven, seed-backed.** The definition comes from the Configuration Engine when a
 * tenant has one and from {@link SEED_AUDIT_WORKFLOW} otherwise. The seed reproduces the previous
 * hardcoded behaviour exactly, so an unconfigured tenant sees no change at all.
 * </p>
 *
 * <p>
 * **All or nothing, per part.** Stages and surfaces are taken whole. A lifecycle whose stages
 * reference each other by order, and whose rules reference each other's facts, cannot be merged
 * value-by-value: three tenant stages overlaid on six platform ones is not a lifecycle, it is a
 * corruption of one. A configured set that fails to parse is rejected in favour of the seed, which
 * degrades to platform behaviour rather than to a broken workspace.
 * </p>
 */
@Injectable({ providedIn: 'root' })
export class AuditWorkflowEngine {
  private readonly configuration = inject(PlatformConfigurationService);

  private readonly configuredStages = this.configuration.values(
    CONFIGURATION_CATEGORY.auditWorkflowStage,
  );
  private readonly configuredSurfaces = this.configuration.values(
    CONFIGURATION_CATEGORY.auditWorkflowSurface,
  );

  /** The lifecycle in force for this organization. */
  readonly definition = computed<WorkflowDefinition>(() => ({
    stages: toStages(this.configuredStages()) ?? SEED_AUDIT_WORKFLOW.stages,
    surfaces: toSurfaces(this.configuredSurfaces()) ?? SEED_AUDIT_WORKFLOW.surfaces,
  }));

  /** True when the lifecycle in force came from configuration rather than the seed. */
  readonly isConfigured = computed(() => toStages(this.configuredStages()) !== null);

  /** Every stage, in lifecycle order. */
  readonly stages = computed<readonly AuditWorkflowStage[]>(() => this.definition().stages);

  // ---------------------------------------------------------------------------------------------
  // Position
  // ---------------------------------------------------------------------------------------------

  /** The stage with this id, or `null` when the lifecycle has no such stage. */
  stage(id: AuditWorkflowStageId | null | undefined): AuditWorkflowStage | null {
    if (!id) return null;
    return this.stages().find((s) => s.id === id) ?? null;
  }

  /** Position in the lifecycle, or `-1` for an id the lifecycle does not contain. */
  stageIndex(id: AuditWorkflowStageId | null | undefined): number {
    if (!id) return -1;
    return this.stages().findIndex((s) => s.id === id);
  }

  /** The stage that follows `id`, or `null` at the end of the lifecycle. */
  nextStage(id: AuditWorkflowStageId | null | undefined): AuditWorkflowStage | null {
    const idx = this.stageIndex(id);
    if (idx < 0) return null;
    return this.stages()[idx + 1] ?? null;
  }

  /** The stage before `id`, or `null` at the entry stage. */
  previousStage(id: AuditWorkflowStageId | null | undefined): AuditWorkflowStage | null {
    const idx = this.stageIndex(id);
    if (idx <= 0) return null;
    return this.stages()[idx - 1] ?? null;
  }

  /**
   * Which stage a capability tab belongs to.
   *
   * Tabs outside the lifecycle (overview, intelligence, administration) return `null` — they are
   * vantage points over the workflow, not steps in it, so the progress bar reports no current stage
   * while the user is in them.
   */
  stageForTab(tab: string | null | undefined): AuditWorkflowStage | null {
    if (!tab) return null;
    return this.stages().find((s) => s.tab === tab) ?? null;
  }

  /**
   * Positional progress relative to the stage the user is in.
   *
   * Everything before the current stage reads as complete. This describes where the user is in the
   * sequence, not whether the work actually finished — {@link evaluate} answers that.
   */
  progressTo(current: AuditWorkflowStageId | null): readonly AuditWorkflowStageProgress[] {
    const currentIdx = current ? this.stageIndex(current) : -1;
    return this.stages().map((stage, idx) => ({
      stage,
      status:
        currentIdx < 0 || idx > currentIdx
          ? ('pending' as const)
          : idx === currentIdx
            ? ('current' as const)
            : ('complete' as const),
    }));
  }

  // ---------------------------------------------------------------------------------------------
  // Evaluation
  // ---------------------------------------------------------------------------------------------

  /**
   * Derive the lifecycle from reported facts: completion, outstanding work, blocked stages,
   * recommendations, and where the work actually sits.
   */
  evaluate(facts: AuditLifecycleFacts): AuditLifecycleState {
    return deriveLifecycleState(facts, this.definition());
  }

  // ---------------------------------------------------------------------------------------------
  // Permissions
  // ---------------------------------------------------------------------------------------------

  /**
   * Surfaces of a capability tab this user may reach, in order.
   *
   * Three shell components each held their own copy of this list plus an identical role check. The
   * same declaration now guards the routes, so the workspace cannot advertise a destination the
   * guard would reject — the property the original consolidation was careful to preserve.
   */
  surfacesFor(tab: string, roles: readonly string[]): readonly WorkflowSurfaceDefinition[] {
    return this.definition()
      .surfaces.filter((s) => s.tab === tab && this.permits(s, roles))
      .sort((a, b) => a.order - b.order);
  }

  /** Whether a user holding `roles` may reach `surface`. */
  permits(surface: WorkflowSurfaceDefinition, roles: readonly string[]): boolean {
    const required = surface.requiresAnyRole;
    if (!required || required.length === 0) return true;
    return roles.some((role) => required.includes(role));
  }

  /** Roles that may reach a named surface, or `null` when it is unrestricted or unknown. */
  rolesFor(tab: string, surfaceId: string): readonly string[] | null {
    const surface = this.definition().surfaces.find((s) => s.tab === tab && s.id === surfaceId);
    const required = surface?.requiresAnyRole;
    return required && required.length > 0 ? required : null;
  }
}

// -------------------------------------------------------------------------------------------------
// Mapping configuration into definitions
// -------------------------------------------------------------------------------------------------

/**
 * Map configured values into stage definitions, or `null` when the set is absent or unusable.
 *
 * `null` rather than a partial result: see the class comment on why a lifecycle is taken whole.
 */
function toStages(
  values: readonly ConfigurationValueDescriptor[],
): readonly WorkflowStageDefinition[] | null {
  if (values.length === 0) return null;

  const stages: WorkflowStageDefinition[] = [];

  for (const value of values) {
    const completeWhen = parseWorkflowCondition(value.compound['completeWhen']);
    // A stage with no completion rule can never be finished, which would strand the lifecycle on it
    // forever. Better to reject the whole configured set and stay on known-good behaviour.
    if (!completeWhen) return null;

    const outstanding = toMessageRules(value.compound['outstanding']);
    const recommendations = toMessageRules(value.compound['recommendations']);
    if (!outstanding || !recommendations) return null;

    const entryBlockedWhen = toMessageRule(value.compound['entryBlockedWhen']);
    if (entryBlockedWhen === false) return null;

    stages.push({
      id: value.id,
      label: value.label,
      tab: attr(value, 'tab') ?? value.id,
      route: attr(value, 'route') ?? '',
      order: value.sortOrder,
      consumes: toStringList(value.compound['consumes']),
      produces: toStringList(value.compound['produces']),
      nextActionLabel: attr(value, 'nextActionLabel') ?? '',
      completeWhen,
      outstanding,
      recommendations,
      ...(entryBlockedWhen ? { entryBlockedWhen } : {}),
    });
  }

  return stages.sort((a, b) => a.order - b.order);
}

function toSurfaces(
  values: readonly ConfigurationValueDescriptor[],
): readonly WorkflowSurfaceDefinition[] | null {
  if (values.length === 0) return null;

  const surfaces: WorkflowSurfaceDefinition[] = [];

  for (const value of values) {
    const tab = attr(value, 'tab');
    const route = attr(value, 'route');
    // A surface without a tab cannot be placed and one without a route cannot be reached; either
    // would render as a dead entry in the sub-navigation.
    if (!tab || !route) return null;

    const requiresAnyRole = toStringList(value.compound['requiresAnyRole']);

    surfaces.push({
      // Surface ids repeat across tabs, so the configuration code is `tab.id` and the bare id is
      // carried alongside. Falling back to the code keeps a hand-written row without `surfaceId`
      // usable as long as its code is already unique.
      id: attr(value, 'surfaceId') ?? value.id,
      tab,
      label: value.label,
      route,
      order: value.sortOrder,
      ...(requiresAnyRole.length > 0 ? { requiresAnyRole } : {}),
    });
  }

  return surfaces;
}

function attr(value: ConfigurationValueDescriptor, key: string): string | null {
  const raw = value.attributes[key];
  return typeof raw === 'string' && raw !== '' ? raw : null;
}

function toStringList(raw: unknown): readonly string[] {
  if (!Array.isArray(raw)) return [];
  return raw.filter((v): v is string => typeof v === 'string');
}

/** `null` when absent, `false` when present but malformed — the caller must tell those apart. */
function toMessageRule(raw: unknown): WorkflowMessageRule | null | false {
  if (raw === undefined || raw === null) return null;
  if (typeof raw !== 'object' || Array.isArray(raw)) return false;

  const node = raw as Record<string, unknown>;
  const when: WorkflowCondition | null = parseWorkflowCondition(node['when']);
  if (!when || typeof node['message'] !== 'string') return false;

  return { when, message: node['message'] };
}

/** `null` when the list is present but malformed; an absent list is simply empty. */
function toMessageRules(raw: unknown): readonly WorkflowMessageRule[] | null {
  if (raw === undefined || raw === null) return [];
  if (!Array.isArray(raw)) return null;

  const rules: WorkflowMessageRule[] = [];
  for (const entry of raw) {
    const rule = toMessageRule(entry);
    if (!rule) return null;
    rules.push(rule);
  }
  return rules;
}

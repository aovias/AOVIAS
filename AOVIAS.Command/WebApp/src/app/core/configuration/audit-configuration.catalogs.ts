import { catalogValues, type ConfigurationCatalog } from './configuration-catalog';

/**
 * Seed catalogs for every configurable business definition in the Audit workspace.
 *
 * <p>
 * **What moved here.** Before this file the same definitions lived as `export const` label maps,
 * `<option>` lists and `if (status === …)` chains scattered across the Findings, CAPA, Execution and
 * Reports screens — several of them in three places at once, and already divergent: a deficiency in
 * `ReviewPending` read "Verification Pending" on the lifecycle bar and "Pending Verification" in the
 * filter beside it. Those are not two decisions; they are one decision recorded twice and
 * maintained once.
 * </p>
 *
 * <p>
 * **These are seeds, not defaults-with-a-branch.** Each catalog is the starting content of the store
 * that {@link PlatformConfigurationService} exposes and that the Enterprise Configuration API writes
 * into. No consumer asks whether a category is configured, so the configured and unconfigured paths
 * cannot behave differently.
 * </p>
 *
 * <p>
 * **Attributes over identity.** Consumers must branch on what a value *does*
 * (`selectableWhenFiltering`) rather than on which value it *is* (`=== 'Closed'`). That is what lets
 * an administrator add a status without an Angular change — the success condition for this wave.
 * </p>
 */

/** Category codes as known to `/configuration/categories/{code}`. */
export const CONFIGURATION_CATEGORY = {
  auditType: 'AuditType',
  auditRecurrence: 'AuditRecurrence',
  auditSessionStatus: 'AuditSessionStatus',
  auditSessionResult: 'AuditSessionResult',
  scheduledAuditStatus: 'ScheduledAuditStatus',
  observationType: 'ObservationType',
  reportPeriod: 'ReportPeriod',
  findingStatus: 'FindingStatus',
  findingSource: 'FindingSource',
  findingClassification: 'FindingClassification',
  findingSeverity: 'FindingSeverity',
  findingAttachmentType: 'FindingAttachmentType',
  findingQuickFilter: 'FindingQuickFilter',
  promotionReadiness: 'PromotionReadiness',
  deficiencyStatus: 'DeficiencyStatus',
  deficiencySeverity: 'DeficiencySeverity',
  deficiencyGrouping: 'DeficiencyGrouping',
  riskLevel: 'RiskLevel',
  rootCauseCategory: 'RootCauseCategory',
  capaActionStatus: 'CapaActionStatus',
  effectivenessReviewStatus: 'EffectivenessReviewStatus',
  effectivenessReviewSchedule: 'EffectivenessReviewSchedule',
  maturityStage: 'MaturityStage',
  department: 'Department',
  slaStatus: 'SlaStatus',
  assignmentConfidence: 'AssignmentConfidence',
  auditWorkflowStage: 'AuditWorkflowStage',
  auditWorkflowSurface: 'AuditWorkflowSurface',
} as const;

export type ConfigurationCategoryCode =
  (typeof CONFIGURATION_CATEGORY)[keyof typeof CONFIGURATION_CATEGORY];

/**
 * Audit types.
 *
 * <p>
 * `selectableWhenScheduling` is the difference between "this type exists" and "a planner may choose
 * it": Consultant Review and the NABH assessments are recorded by the system but never scheduled by
 * hand, which the original hardcoded Planning dropdown expressed by simply omitting them, with no
 * way to tell a deliberate exclusion from an oversight.
 * </p>
 *
 * <p>
 * `selectableWhenStartingSession` is a *different* rule that a second hardcoded list expressed the
 * same way: the Auditor sessions screen offers the external assessments (an auditor does execute
 * them) but not Department or Follow-up audits. Two lists, two intents, no way to tell them apart
 * — which is why they are now two attributes.
 * </p>
 */
const AUDIT_TYPE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.auditType,
  description: 'Kinds of audit the platform records, schedules and executes.',
  seed: catalogValues([
    [
      'InternalAudit',
      'Internal',
      { selectableWhenScheduling: true, selectableWhenStartingSession: true },
    ],
    ['DepartmentAudit', 'Department', { selectableWhenScheduling: true }],
    ['FollowUpAudit', 'Follow-up', { selectableWhenScheduling: true }],
    [
      'MockAudit',
      'Mock NABH',
      { selectableWhenScheduling: true, selectableWhenStartingSession: true },
    ],
    [
      'FocusedAudit',
      'Focused',
      { selectableWhenScheduling: true, selectableWhenStartingSession: true },
    ],
    [
      'Surveillance',
      'Surveillance',
      { selectableWhenScheduling: true, selectableWhenStartingSession: true, surveillance: true },
    ],
    [
      'ConsultantReview',
      'Consultant Review',
      { selectableWhenScheduling: false, selectableWhenStartingSession: true, external: true },
    ],
    [
      'NabhPreAssessment',
      'NABH Pre-Assessment',
      { selectableWhenScheduling: false, selectableWhenStartingSession: true, external: true },
    ],
    [
      'NabhFinalAssessment',
      'NABH Final Assessment',
      { selectableWhenScheduling: false, selectableWhenStartingSession: true, external: true },
    ],
  ]),
};

/**
 * Recurrence offered when scheduling a programme of audits.
 *
 * `occurrences` and `intervalMonths` are what Planning actually needs; the label carries the count
 * only so the user can read it. Previously both the count and the interval were implied by the
 * option's *name* ("Monthly ×3") and re-derived in a `switch` — so changing the cadence meant
 * editing a string and a branch and hoping they agreed.
 */
const AUDIT_RECURRENCE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.auditRecurrence,
  description: 'Repeat cadence offered when scheduling an audit programme.',
  seed: catalogValues([
    ['none', 'None', { occurrences: 1, intervalMonths: 0 }],
    ['monthly', 'Monthly ×3', { occurrences: 3, intervalMonths: 1 }],
    ['quarterly', 'Quarterly ×4', { occurrences: 4, intervalMonths: 3 }],
  ]),
};

/**
 * Execution session lifecycle.
 *
 * Six screens across Audit and Auditor read these labels; all six imported the same map from a
 * feature model, which made a *feature* the owner of a platform-wide business definition.
 */
const AUDIT_SESSION_STATUS: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.auditSessionStatus,
  description: 'Lifecycle states of an audit execution session.',
  seed: catalogValues([
    ['Planned', 'Planned', { lifecycleStep: true, selectableWhenFiltering: true }],
    ['InProgress', 'In Progress', { lifecycleStep: true, selectableWhenFiltering: true }],
    ['Review', 'Review', { lifecycleStep: true, selectableWhenFiltering: true }],
    ['DraftReport', 'Draft Report', { lifecycleStep: true, selectableWhenFiltering: true }],
    [
      'Submitted',
      'Submitted',
      { lifecycleStep: true, selectableWhenFiltering: true, terminal: true },
    ],
    ['Closed', 'Closed', { lifecycleStep: true, selectableWhenFiltering: true, terminal: true }],
  ]),
};

/**
 * States of a *scheduled* audit, as distinct from the session that executes it.
 *
 * `terminal` decides what counts as overdue: Surveillance held this as a private `readonly` tuple,
 * so "which states mean the work is finished" was a decision only that one screen could see.
 */
const SCHEDULED_AUDIT_STATUS: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.scheduledAuditStatus,
  description: 'States of an audit on the schedule, before and after execution.',
  seed: catalogValues([
    ['Scheduled', 'Scheduled'],
    ['InProgress', 'In Progress'],
    ['Completed', 'Completed', { terminal: true }],
    ['Closed', 'Closed', { terminal: true }],
    // Not terminal, matching the tuple this replaced — so a cancelled audit past its date still
    // counts as overdue, exactly as before. That looks wrong, but it is now a configuration
    // decision an administrator can make rather than one buried in a private field.
    ['Cancelled', 'Cancelled'],
  ]),
};

/** Compliance verdict an execution session concludes with. */
const AUDIT_SESSION_RESULT: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.auditSessionResult,
  description: 'Compliance verdict recorded when an audit session closes.',
  seed: catalogValues([
    ['NotDetermined', 'Not Determined'],
    ['Compliant', 'Compliant'],
    ['ConditionallyCompliant', 'Conditionally Compliant'],
    ['PartiallyCompliant', 'Partially Compliant'],
    ['NonCompliant', 'Non-Compliant'],
  ]),
};

/** Kinds of observation an auditor records during execution. */
const OBSERVATION_TYPE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.observationType,
  description: 'Kinds of observation recorded during an audit session.',
  seed: catalogValues([
    ['PositivePractice', 'Positive Practice'],
    ['Suggestion', 'Suggestion'],
    ['MinorObservation', 'Minor Observation'],
    ['Strength', 'Strength'],
    ['Opportunity', 'Opportunity'],
    ['GeneralComment', 'General Comment'],
  ]),
};

/** Reporting windows offered on the Reports screen. */
const REPORT_PERIOD: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.reportPeriod,
  description: 'Time windows offered when generating audit reports.',
  seed: catalogValues([
    ['programYear', 'Current programme year'],
    ['quarter', 'Last 3 months'],
    ['month', 'Last 30 days'],
  ]),
};

/**
 * Finding lifecycle statuses.
 *
 * <p>
 * Three attributes, because the previous code needed three separate lists to say the same things:
 * `FINDING_STATUS_LABELS` (display), `FINDING_STATUS_TRANSITIONS` (what a reviewer may set) and a
 * hardcoded `<option>` block in the page (what may be filtered). `InProgress` and `Verified`
 * appeared only in the first, which is why a finding in either state could not be filtered for — a
 * known defect that is now a configuration toggle rather than a code change.
 * </p>
 *
 * <p>
 * `detailStep` orders the detail panel's rail, which includes `Promoted` — a waypoint the workflow
 * passes through but never a persisted status, hence `persisted: false` and every selectable flag
 * off. It cannot leak into a picker because every picker filters on those flags.
 * </p>
 */
const FINDING_STATUS: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.findingStatus,
  description: 'Lifecycle states a finding moves through.',
  seed: catalogValues([
    [
      'Draft',
      'Draft',
      {
        lifecycleStep: true,
        detailStep: true,
        selectableWhenFiltering: true,
        selectableWhenTransitioning: true,
        selectableWhenCreating: true,
      },
    ],
    [
      'Open',
      'Open',
      {
        lifecycleStep: true,
        detailStep: true,
        selectableWhenFiltering: true,
        selectableWhenTransitioning: true,
        selectableWhenCreating: true,
      },
    ],
    [
      'Reviewed',
      'Reviewed',
      {
        lifecycleStep: true,
        detailStep: true,
        selectableWhenFiltering: true,
        selectableWhenTransitioning: true,
      },
    ],
    [
      'Accepted',
      'Accepted',
      {
        lifecycleStep: true,
        detailStep: true,
        selectableWhenFiltering: true,
        selectableWhenTransitioning: true,
      },
    ],
    ['Promoted', 'Promoted', { detailStep: true, persisted: false }],
    [
      'Closed',
      'Closed',
      {
        lifecycleStep: true,
        detailStep: true,
        selectableWhenFiltering: true,
        selectableWhenTransitioning: true,
      },
    ],
    ['InProgress', 'In Progress', {}],
    ['Verified', 'Verified', {}],
  ]),
};

/**
 * Where a finding came from.
 *
 * `ordinal` mirrors `Shared.Infrastructure/Domain/Enums/FindingRaisedBy.cs`, which **starts at 1**.
 * A row holding `0` — outside the enum — serialises as the literal `"0"` and previously reached the
 * user as a bare `0` in the SOURCE column. Display order is not enum order, which is precisely why
 * the ordinal has to be recorded rather than inferred from position.
 */
const FINDING_SOURCE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.findingSource,
  description: 'Origin of a finding, as raised.',
  seed: catalogValues([
    ['InternalAudit', 'Internal Audit', { ordinal: 1 }],
    ['ExternalAudit', 'External Audit', { ordinal: 2 }],
    ['ConsultantAssessment', 'Consultant Assessment', { ordinal: 4 }],
    ['NabhAssessment', 'NABH Assessment', { ordinal: 5 }],
    ['ManagementReview', 'Management Review', { ordinal: 6 }],
    ['DepartmentAssessment', 'Department Assessment', { ordinal: 7 }],
    ['IncidentReview', 'Incident Investigation', { ordinal: 8 }],
    ['ComplianceTeam', 'Compliance Team', { ordinal: 3 }],
  ]),
};

/**
 * What kind of thing the finding is.
 *
 * `shortLabel` is the grid's compact form. The grid previously derived it by inserting spaces before
 * capitals and then string-replacing the longest result with "OFI" — which read as a formatting
 * quirk but was a display-name decision, and one that made the grid and the filter beside it
 * disagree ("Non Conformity" versus "Non-Conformity").
 */
const FINDING_CLASSIFICATION: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.findingClassification,
  description: 'Category of finding raised during audit.',
  seed: catalogValues([
    ['AuditFinding', 'Audit Finding'],
    ['Observation', 'Observation'],
    ['NonConformity', 'Non-Conformity'],
    ['OpportunityForImprovement', 'Opportunity For Improvement', { shortLabel: 'OFI' }],
    ['GoodPractice', 'Good Practice'],
  ]),
};

/**
 * Finding severity.
 *
 * `badgeToken` is the severity's visual identity. Templates previously built it inline with
 * `'sev--' + row.severity.toLowerCase()`, which meant a severity whose code was not already a valid
 * CSS token rendered unstyled.
 */
const FINDING_SEVERITY: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.findingSeverity,
  description: 'How serious a finding is.',
  seed: catalogValues([
    ['Critical', 'Critical', { badgeToken: 'critical' }],
    ['Major', 'Major', { badgeToken: 'major' }],
    ['Moderate', 'Moderate', { badgeToken: 'moderate' }],
    ['Minor', 'Minor', { badgeToken: 'minor' }],
    ['Informational', 'Informational', { badgeToken: 'informational' }],
  ]),
};

/** Kinds of file attached to a finding. */
const FINDING_ATTACHMENT_TYPE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.findingAttachmentType,
  description: 'Kinds of evidence attachable to a finding.',
  seed: catalogValues([
    ['Document', 'Document'],
    ['Image', 'Image'],
    ['AuditNote', 'Audit Note'],
    ['Report', 'Report'],
    ['Screenshot', 'Screenshot'],
  ]),
};

/** One-click filter chips above the findings grid. */
const FINDING_QUICK_FILTER: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.findingQuickFilter,
  description: 'Saved filters offered as chips on the findings workbench.',
  seed: catalogValues([
    ['needsReview', 'Needs Review'],
    ['readyForPromotion', 'Ready For Promotion'],
    ['recentlyAdded', 'Recently Added'],
    ['critical', 'Critical'],
  ]),
};

/**
 * How close a finding is to becoming a CAPA.
 *
 * Ids carry spaces because they are the values the API filters on, not tokens we chose.
 */
const PROMOTION_READINESS: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.promotionReadiness,
  description: 'Readiness of a finding for promotion to a deficiency.',
  seed: catalogValues([
    ['Not Eligible', 'Not Eligible'],
    ['Awaiting Acceptance', 'Awaiting Acceptance'],
    ['Eligible', 'Eligible'],
    ['Promoted', 'Promoted'],
  ]),
};

/**
 * Deficiency (CAPA) lifecycle statuses.
 *
 * <p>
 * **Unification note.** Three places named these states and two disagreed: the lifecycle bar read
 * "Awaiting Evidence" / "Verification Pending" while the filter beside it read "Evidence Submitted"
 * / "Pending Verification", and a `formatStatus` chain repeated in three components translated a
 * third subset. The lifecycle labels win here because they match `DeficiencyLifecycleStatus`, the
 * shape the API itself emits.
 * </p>
 *
 * <p>
 * `Cancelled` keeps `selectableWhenFiltering: false` to preserve today's filter contents; it is now
 * a toggle rather than an omission.
 * </p>
 */
const DEFICIENCY_STATUS: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.deficiencyStatus,
  description: 'Lifecycle states a deficiency moves through.',
  seed: catalogValues([
    ['Identified', 'Identified', { lifecycleStep: true, selectableWhenFiltering: true }],
    [
      'Assigned',
      'Assigned',
      {
        lifecycleStep: true,
        selectableWhenFiltering: true,
        // The state in which an owner may accept or reject the assignment. The CAPA detail panel
        // tested `status === 'Assigned'` directly (DEF-CFG-002), so renaming or re-sequencing this
        // status silently removed the accept/reject controls.
        awaitingOwnerAcceptance: true,
      },
    ],
    ['InProgress', 'In Progress', { lifecycleStep: true, selectableWhenFiltering: true }],
    [
      'EvidenceSubmitted',
      'Awaiting Evidence',
      { lifecycleStep: true, selectableWhenFiltering: true },
    ],
    [
      'ReviewPending',
      'Verification Pending',
      { lifecycleStep: true, selectableWhenFiltering: true },
    ],
    ['Verified', 'Verified', { lifecycleStep: true, selectableWhenFiltering: true }],
    ['Closed', 'Closed', { lifecycleStep: true, selectableWhenFiltering: true }],
    ['Cancelled', 'Cancelled', {}],
  ]),
};

/** Deficiency severity. Narrower than finding severity — a deficiency is never informational. */
const DEFICIENCY_SEVERITY: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.deficiencySeverity,
  description: 'How serious a deficiency is.',
  seed: catalogValues([
    ['Critical', 'Critical', { badgeToken: 'critical' }],
    ['Major', 'Major', { badgeToken: 'major' }],
    ['Moderate', 'Moderate', { badgeToken: 'moderate' }],
    ['Minor', 'Minor', { badgeToken: 'minor' }],
  ]),
};

/**
 * Grouping offered by the CAPA toolbar.
 *
 * `field` is the row property the grouping reads and `labelFromCategory` names the category that
 * labels the resulting headings. Both were previously implied by a chain of `groupBy === …` tests,
 * which is why only the status grouping produced readable headings and the other two showed raw
 * codes.
 */
const DEFICIENCY_GROUPING: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.deficiencyGrouping,
  description: 'Ways the CAPA workspace may group its rows.',
  seed: catalogValues([
    [
      'severity',
      'Group by Severity',
      { field: 'severity', labelFromCategory: CONFIGURATION_CATEGORY.deficiencySeverity },
    ],
    [
      'stage',
      'Group by Stage',
      { field: 'maturityLevel', labelFromCategory: CONFIGURATION_CATEGORY.maturityStage },
    ],
    [
      'status',
      'Group by Status',
      { field: 'status', labelFromCategory: CONFIGURATION_CATEGORY.deficiencyStatus },
    ],
  ]),
};

/**
 * Assessed risk rating. `badgeClass` replaces the `RISK_LEVEL_CLASS` lookup map.
 *
 * `minScore` is the lower bound of the band on the likelihood × impact scale. The CAPA panel held
 * these thresholds as an `if` ladder (DEF-CFG-003), so an organization using a different risk matrix
 * — a 3×3 instead of a 5×5, say — could rename the bands but not move them, and every deficiency
 * would land in the wrong one.
 */
const RISK_LEVEL: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.riskLevel,
  description: 'Assessed risk rating of a deficiency, and the score band that produces it.',
  seed: catalogValues([
    ['Low', 'Low', { badgeClass: 'risk--low', minScore: 0, tone: 'success' }],
    ['Medium', 'Medium', { badgeClass: 'risk--medium', minScore: 6, tone: 'warning' }],
    ['High', 'High', { badgeClass: 'risk--high', minScore: 12, tone: 'danger' }],
    ['Critical', 'Critical', { badgeClass: 'risk--critical', minScore: 20, tone: 'danger' }],
  ]),
};

/** Root cause taxonomy used by the five-whys workspace. */
const ROOT_CAUSE_CATEGORY: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.rootCauseCategory,
  description: 'Root cause taxonomy for deficiency analysis.',
  seed: catalogValues([
    ['PolicyMissing', 'Policy Missing'],
    ['EvidenceMissing', 'Evidence Missing'],
    ['TrainingMissing', 'Training Missing'],
    ['ProcessFailure', 'Process Failure'],
    ['DocumentationIssue', 'Documentation Issue'],
    ['VerificationFailure', 'Verification Failure'],
    ['Other', 'Other'],
  ]),
};

/** Progress of a single corrective or preventive action. */
const CAPA_ACTION_STATUS: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.capaActionStatus,
  description: 'Progress of a corrective or preventive action.',
  seed: catalogValues([
    ['NotStarted', 'Not Started'],
    ['InProgress', 'In Progress'],
    ['Completed', 'Completed'],
  ]),
};

/** Outcome of the post-closure effectiveness review. */
const EFFECTIVENESS_REVIEW_STATUS: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.effectivenessReviewStatus,
  description: 'Outcome of a CAPA effectiveness review.',
  seed: catalogValues([
    ['NotRequired', 'Not Required'],
    ['Pending', 'Pending'],
    ['Scheduled', 'Scheduled'],
    ['Effective', 'Effective'],
    ['NotEffective', 'Not Effective'],
  ]),
};

/**
 * Offsets at which an effectiveness review may be scheduled.
 *
 * `days` is the value the form submits; the label merely renders it. The old constant carried the
 * number in both, so "60 Day Review" scheduling 90 days out was a one-character typo away.
 */
const EFFECTIVENESS_REVIEW_SCHEDULE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.effectivenessReviewSchedule,
  description: 'Offsets at which a CAPA effectiveness review may be scheduled.',
  seed: catalogValues([
    ['30', '30 Day Review', { days: 30 }],
    ['60', '60 Day Review', { days: 60 }],
    ['90', '90 Day Review', { days: 90 }],
  ]),
};

/** NABH accreditation stages a deficiency can block. */
const MATURITY_STAGE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.maturityStage,
  description: 'Accreditation maturity stages a deficiency may block.',
  seed: catalogValues([
    ['Core', 'Core'],
    ['Commitment', 'Commitment'],
    ['Achievement', 'Achievement'],
    ['Excellence', 'Excellence'],
  ]),
};

/**
 * Departments a finding may be attributed to.
 *
 * Ordered once here. The two screens offering this list previously ordered it differently, so the
 * same dropdown read `HR, Security, Clinical, Admin` in the workbench and
 * `Clinical, HR, Security, Admin` in the capture panel.
 */
const DEPARTMENT: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.department,
  description: 'Departments findings and deficiencies are attributed to.',
  seed: catalogValues([
    ['hr', 'HR'],
    ['security', 'Security'],
    ['clinical', 'Clinical'],
    ['admin', 'Admin'],
  ]),
};

/**
 * SLA states for a deficiency's remediation clock.
 *
 * Replaces an `if/else` ladder duplicated in the CAPA grid and detail panel, whose final `else`
 * silently rendered every unrecognised state — including a genuinely new one — as "closed".
 */
const SLA_STATUS: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.slaStatus,
  description: 'States of a deficiency remediation SLA clock.',
  seed: catalogValues([
    ['OnTrack', 'On Track', { badgeClass: 'sla sla--ontrack' }],
    ['Warning', 'Warning', { badgeClass: 'sla sla--warning' }],
    ['AtRisk', 'At Risk', { badgeClass: 'sla sla--atrisk' }],
    ['Breached', 'Breached', { badgeClass: 'sla sla--breached' }],
    ['Closed', 'Closed', { badgeClass: 'sla sla--closed', fallback: true }],
  ]),
};

/** Confidence the routing engine has in an auto-assignment suggestion. */
const ASSIGNMENT_CONFIDENCE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.assignmentConfidence,
  description: 'Confidence of an automated assignment suggestion.',
  seed: catalogValues([
    ['None', 'None', { badgeClass: 'conf conf--none', fallback: true }],
    ['Low', 'Low', { badgeClass: 'conf conf--low' }],
    ['Medium', 'Medium', { badgeClass: 'conf conf--medium' }],
    ['High', 'High', { badgeClass: 'conf conf--high' }],
  ]),
};

/**
 * The audit lifecycle, as configuration.
 *
 * <p>
 * **Seeded empty on purpose.** Every other category here seeds its platform defaults, because a
 * label is a value and values merge one at a time. A lifecycle does not: its stages reference each
 * other through ordering and their rules reference each other's facts, so a half-configured
 * lifecycle — three tenant stages overlaid on six platform ones — is not a lifecycle. The engine
 * therefore takes a configured definition whole or not at all, and the platform default lives with
 * the definition it belongs to, in `workflow-definition.model.ts`.
 * </p>
 *
 * <p>
 * Registering the codes here is still what makes them *reachable*: `load()` only requests categories
 * it knows about. An empty seed means "the platform default is not expressed as lookup values",
 * not "there is no default".
 * </p>
 *
 * <p>
 * Rules ride in each value's `metadata` as JSON objects, which is why
 * {@link ConfigurationValueDescriptor.compound} exists — a condition is a tree, not a scalar.
 * </p>
 */
const AUDIT_WORKFLOW_STAGE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.auditWorkflowStage,
  description: 'Stages of the audit lifecycle and the rules that govern each.',
  seed: [],
};

/** Sub-navigation surfaces within the audit capability tabs, with their role restrictions. */
const AUDIT_WORKFLOW_SURFACE: ConfigurationCatalog = {
  code: CONFIGURATION_CATEGORY.auditWorkflowSurface,
  description: 'Sub-navigation surfaces within audit capability tabs.',
  seed: [],
};

/**
 * Every catalog the Audit workspace consumes.
 *
 * {@link PlatformConfigurationService} seeds its store from this list and asks the API only for the
 * categories the platform reports as existing, so an unregistered category costs no request.
 */
export const AUDIT_CONFIGURATION_CATALOGS: readonly ConfigurationCatalog[] = [
  AUDIT_TYPE,
  AUDIT_RECURRENCE,
  AUDIT_SESSION_STATUS,
  AUDIT_SESSION_RESULT,
  SCHEDULED_AUDIT_STATUS,
  OBSERVATION_TYPE,
  REPORT_PERIOD,
  FINDING_STATUS,
  FINDING_SOURCE,
  FINDING_CLASSIFICATION,
  FINDING_SEVERITY,
  FINDING_ATTACHMENT_TYPE,
  FINDING_QUICK_FILTER,
  PROMOTION_READINESS,
  DEFICIENCY_STATUS,
  DEFICIENCY_SEVERITY,
  DEFICIENCY_GROUPING,
  RISK_LEVEL,
  ROOT_CAUSE_CATEGORY,
  CAPA_ACTION_STATUS,
  EFFECTIVENESS_REVIEW_STATUS,
  EFFECTIVENESS_REVIEW_SCHEDULE,
  MATURITY_STAGE,
  DEPARTMENT,
  SLA_STATUS,
  ASSIGNMENT_CONFIDENCE,
  AUDIT_WORKFLOW_STAGE,
  AUDIT_WORKFLOW_SURFACE,
];

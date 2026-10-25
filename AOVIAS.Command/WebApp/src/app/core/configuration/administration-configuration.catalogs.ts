import { catalogValues, type ConfigurationCatalog } from './configuration-catalog';

/**
 * Configuration domains owned by Enterprise Administration.
 *
 * <p>
 * A second catalog module, not a second mechanism. These register into the same store, load through
 * the same API, resolve through the same tenant-override rules and are edited by the same generic
 * editor as the Audit domains. The split is by *ownership* — Audit metadata describes how an audit
 * behaves, these describe how the platform behaves around it — and keeps either file readable.
 * </p>
 *
 * <p>
 * **Consumer status is recorded per domain.** Some of these back rules that exist in code today and
 * are migrated onto them in this wave; others are declared ahead of the surfaces that will read
 * them. That distinction is real and is stated on each catalog rather than glossed, because a
 * category nothing reads is data, not behaviour — it is administrable but it does not yet change
 * anything.
 * </p>
 */

/** Category codes for the Administration domains. */
export const ADMINISTRATION_CATEGORY = {
  // Risk
  riskLikelihood: 'RiskLikelihood',
  riskImpact: 'RiskImpact',
  riskCategory: 'RiskCategory',
  riskEscalationThreshold: 'RiskEscalationThreshold',
  // Approval
  approvalLevel: 'ApprovalLevel',
  approvalOutcome: 'ApprovalOutcome',
  // Notification
  notificationCategory: 'NotificationCategory',
  notificationChannel: 'NotificationChannel',
  notificationPriority: 'NotificationPriority',
  // SLA
  slaPolicy: 'SlaPolicy',
  slaWarningThreshold: 'SlaWarningThreshold',
  businessCalendar: 'BusinessCalendar',
  // Evidence
  evidenceCategory: 'EvidenceCategory',
  evidenceType: 'EvidenceType',
  evidenceRetentionPolicy: 'EvidenceRetentionPolicy',
  // Document
  documentType: 'DocumentType',
  documentClassification: 'DocumentClassification',
  documentRetentionPolicy: 'DocumentRetentionPolicy',
  // Dashboard
  executiveKpi: 'ExecutiveKpi',
  dashboardCard: 'DashboardCard',
} as const;

export type AdministrationCategoryCode =
  (typeof ADMINISTRATION_CATEGORY)[keyof typeof ADMINISTRATION_CATEGORY];

// =================================================================================================
// Phase 1 — Risk.  Consumers: risk matrix page, risk form, CAPA detail panel.
// =================================================================================================

/**
 * Likelihood scale.
 *
 * <p>
 * `level` is the number that enters the score; the code and label are what the user reads. The risk
 * module hardcoded `[1, 2, 3, 4, 5]` in two components and the risk form hardcoded the same range in
 * its `min`/`max`, so an organization on a 3×3 or 4×4 matrix could not be represented at all — the
 * Administration placeholder page said as much in prose ("fixed at 5×5").
 * </p>
 *
 * <p>
 * The scale's *size* is therefore configuration: remove two values and the matrix is 3×3, and the
 * maximum score follows from the highest levels rather than from a constant.
 * </p>
 */
const RISK_LIKELIHOOD: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.riskLikelihood,
  description: 'Likelihood scale for risk scoring. Removing values resizes the matrix.',
  seed: catalogValues([
    ['1', 'Rare', { level: 1 }],
    ['2', 'Unlikely', { level: 2 }],
    ['3', 'Possible', { level: 3 }],
    ['4', 'Likely', { level: 4 }],
    ['5', 'Almost Certain', { level: 5 }],
  ]),
};

/** Impact scale. Same shape and the same reasoning as likelihood. */
const RISK_IMPACT: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.riskImpact,
  description: 'Impact scale for risk scoring. Removing values resizes the matrix.',
  seed: catalogValues([
    ['1', 'Insignificant', { level: 1 }],
    ['2', 'Minor', { level: 2 }],
    ['3', 'Moderate', { level: 3 }],
    ['4', 'Major', { level: 4 }],
    ['5', 'Catastrophic', { level: 5 }],
  ]),
};

/**
 * Risk taxonomy.
 *
 * Declared here rather than derived from the register's free-text `category` field so an
 * administrator can constrain what may be recorded. No consumer reads it yet — the risk form
 * currently accepts free text.
 */
const RISK_CATEGORY: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.riskCategory,
  description: 'Taxonomy a risk may be classified under.',
  seed: catalogValues([
    ['PatientSafety', 'Patient Safety'],
    ['Clinical', 'Clinical'],
    ['Operational', 'Operational'],
    ['Regulatory', 'Regulatory'],
    ['Financial', 'Financial'],
    ['InformationSecurity', 'Information Security'],
    ['Reputational', 'Reputational'],
    ['Strategic', 'Strategic'],
  ]),
};

/**
 * Score at which a risk escalates, and to whom.
 *
 * **No client consumer today.** Escalation is currently decided server-side and the client only
 * renders the resulting `escalationLevel` and `escalationState`. This catalog gives an administrator
 * somewhere to state the intent and gives that server rule a single published source; nothing in the
 * web client branches on it yet.
 */
const RISK_ESCALATION_THRESHOLD: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.riskEscalationThreshold,
  description: 'Score thresholds that trigger escalation, and the level they escalate to.',
  seed: catalogValues([
    ['Level1', 'Department Head', { minScore: 12, escalationLevel: 1 }],
    ['Level2', 'Quality Committee', { minScore: 16, escalationLevel: 2 }],
    ['Level3', 'Executive', { minScore: 20, escalationLevel: 3 }],
  ]),
};

// =================================================================================================
// Phase 2 — Approval.  No client consumer today; the approval surfaces are placeholders.
// =================================================================================================

/**
 * Approval levels, in the order they are sought.
 *
 * `timeoutHours` and `escalatesToLevel` express the timeout and escalation rules the brief names;
 * `delegable` expresses delegation. **No client consumer today** — Administration's approvals tab is
 * a placeholder and no workflow surface requests an approval.
 */
const APPROVAL_LEVEL: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.approvalLevel,
  description: 'Approval levels, their roles, timeouts and escalation.',
  seed: catalogValues([
    [
      'DepartmentHead',
      'Department Head',
      { requiresRole: 'DEPARTMENT_HEAD', timeoutHours: 72, delegable: true, escalatesToLevel: 2 },
    ],
    [
      'QualityLead',
      'Quality Lead',
      { requiresRole: 'QUALITY_LEAD', timeoutHours: 72, delegable: true, escalatesToLevel: 3 },
    ],
    ['Executive', 'Executive', { requiresRole: 'ORG_ADMIN', timeoutHours: 120, delegable: false }],
  ]),
};

/** Outcomes an approver may record. No client consumer today. */
const APPROVAL_OUTCOME: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.approvalOutcome,
  description: 'Decisions an approver may record.',
  seed: catalogValues([
    ['Approved', 'Approved', { terminal: true, advances: true }],
    ['Rejected', 'Rejected', { terminal: true, advances: false }],
    ['ChangesRequested', 'Changes Requested', { terminal: false, advances: false }],
    ['Delegated', 'Delegated', { terminal: false, advances: false }],
  ]),
};

// =================================================================================================
// Phase 3 — Notification.  Consumer: the notifications admin module (routes exist).
// =================================================================================================

/** What a notification is about. Drives subscription grouping. */
const NOTIFICATION_CATEGORY: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.notificationCategory,
  description: 'Subject areas a user may subscribe to notifications for.',
  seed: catalogValues([
    ['AuditScheduled', 'Audit Scheduled', { defaultChannel: 'InApp' }],
    ['FindingRaised', 'Finding Raised', { defaultChannel: 'InApp' }],
    ['CapaAssigned', 'CAPA Assigned', { defaultChannel: 'Email' }],
    ['CapaOverdue', 'CAPA Overdue', { defaultChannel: 'Email' }],
    ['EvidenceRejected', 'Evidence Rejected', { defaultChannel: 'InApp' }],
    ['SlaBreached', 'SLA Breached', { defaultChannel: 'Email' }],
    ['ApprovalRequested', 'Approval Requested', { defaultChannel: 'Email' }],
    ['ReportAvailable', 'Report Available', { defaultChannel: 'InApp' }],
  ]),
};

/**
 * Delivery channels.
 *
 * `retryAttempts` and `retryBackoffMinutes` carry the retry rules the brief names. `quietHoursApply`
 * marks the channels a quiet-hours window suppresses — in-app messages do not interrupt anyone, so
 * suppressing them would only hide information.
 */
const NOTIFICATION_CHANNEL: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.notificationChannel,
  description: 'Delivery channels, their retry behaviour and quiet-hours treatment.',
  seed: catalogValues([
    ['InApp', 'In-App', { retryAttempts: 0, quietHoursApply: false }],
    ['Email', 'Email', { retryAttempts: 3, retryBackoffMinutes: 15, quietHoursApply: true }],
    ['Sms', 'SMS', { retryAttempts: 2, retryBackoffMinutes: 5, quietHoursApply: true }],
  ]),
};

/** Notification priority. `bypassesQuietHours` is what makes a priority meaningful. */
const NOTIFICATION_PRIORITY: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.notificationPriority,
  description: 'Notification priority and its treatment during quiet hours.',
  seed: catalogValues([
    ['Low', 'Low', { bypassesQuietHours: false }],
    ['Normal', 'Normal', { bypassesQuietHours: false }],
    ['High', 'High', { bypassesQuietHours: false }],
    ['Critical', 'Critical', { bypassesQuietHours: true }],
  ]),
};

// =================================================================================================
// Phase 4 — SLA.  Partial consumer: SlaStatus is already configured and read by the CAPA surfaces.
// =================================================================================================

/**
 * Remediation SLA policies by severity.
 *
 * `resolutionHours` is the clock; `businessHoursOnly` decides whether it runs against the calendar
 * below or wall-clock. **No client consumer today** — the SLA clock is computed server-side and the
 * client renders `slaStatus`, `slaDueDate` and `daysRemaining` as given.
 */
const SLA_POLICY: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.slaPolicy,
  description: 'Remediation time allowed, by severity.',
  seed: catalogValues([
    ['Critical', 'Critical', { resolutionHours: 24, businessHoursOnly: false }],
    ['Major', 'Major', { resolutionHours: 72, businessHoursOnly: true }],
    ['Moderate', 'Moderate', { resolutionHours: 168, businessHoursOnly: true }],
    ['Minor', 'Minor', { resolutionHours: 336, businessHoursOnly: true }],
  ]),
};

/**
 * Points in an SLA at which the state changes or a reminder is sent.
 *
 * `percentConsumed` is what makes this a threshold rather than a fixed time, so it holds for a
 * 24-hour and a 336-hour policy alike. The states correspond to the already-configured `SlaStatus`
 * values, which is where the client reads them from.
 */
const SLA_WARNING_THRESHOLD: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.slaWarningThreshold,
  description: 'Proportion of an SLA consumed before each warning state and reminder.',
  seed: catalogValues([
    ['Warning', 'Warning', { percentConsumed: 50, slaStatus: 'Warning', sendsReminder: true }],
    ['AtRisk', 'At Risk', { percentConsumed: 80, slaStatus: 'AtRisk', sendsReminder: true }],
    ['Breached', 'Breached', { percentConsumed: 100, slaStatus: 'Breached', sendsReminder: true }],
  ]),
};

/**
 * Working calendar.
 *
 * One value per weekday so an organization with a six-day week, or different Friday hours, is
 * expressible without a bespoke schema. Holidays are dates rather than weekdays and belong in their
 * own tenant records, not here — a lookup category keyed by code is the wrong shape for a list that
 * changes every year. **No client consumer today.**
 */
const BUSINESS_CALENDAR: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.businessCalendar,
  description: 'Working days and hours used when an SLA runs on business hours.',
  seed: catalogValues([
    ['Monday', 'Monday', { working: true, startHour: 9, endHour: 17 }],
    ['Tuesday', 'Tuesday', { working: true, startHour: 9, endHour: 17 }],
    ['Wednesday', 'Wednesday', { working: true, startHour: 9, endHour: 17 }],
    ['Thursday', 'Thursday', { working: true, startHour: 9, endHour: 17 }],
    ['Friday', 'Friday', { working: true, startHour: 9, endHour: 17 }],
    ['Saturday', 'Saturday', { working: false }],
    ['Sunday', 'Sunday', { working: false }],
  ]),
};

// =================================================================================================
// Phase 5 — Evidence.  Consumer: the finding attachment picker already reads FindingAttachmentType.
// =================================================================================================

/** Broad grouping of evidence, above the individual type. */
const EVIDENCE_CATEGORY: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.evidenceCategory,
  description: 'Groupings evidence is filed under.',
  seed: catalogValues([
    ['Policy', 'Policy & Procedure'],
    ['Record', 'Clinical & Operational Record'],
    ['Training', 'Training & Competency'],
    ['Audit', 'Audit & Inspection'],
    ['Certification', 'Certification & Licence'],
    ['Photographic', 'Photographic'],
  ]),
};

/**
 * Kinds of evidence, and the rules attached to each.
 *
 * `mandatory` expresses the mandatory-evidence rule, `requiresApproval` the approval requirement and
 * `retentionPolicy` points at the policy below. Keeping retention as a *reference* rather than a
 * duration means changing a retention period is one edit, not one per evidence type.
 */
const EVIDENCE_TYPE: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.evidenceType,
  description: 'Kinds of evidence and the rules that govern each.',
  seed: catalogValues([
    ['Document', 'Document', { requiresApproval: true, retentionPolicy: 'Standard' }],
    ['Photograph', 'Photograph', { requiresApproval: false, retentionPolicy: 'Standard' }],
    ['Certificate', 'Certificate', { requiresApproval: true, retentionPolicy: 'Extended' }],
    ['TrainingRecord', 'Training Record', { requiresApproval: true, retentionPolicy: 'Extended' }],
    ['Screenshot', 'Screenshot', { requiresApproval: false, retentionPolicy: 'Short' }],
    ['AuditNote', 'Audit Note', { requiresApproval: false, retentionPolicy: 'Standard' }],
    [
      'SignedAttestation',
      'Signed Attestation',
      { requiresApproval: true, mandatory: true, retentionPolicy: 'Permanent' },
    ],
  ]),
};

/** How long evidence is kept. Referenced by evidence type and document type alike. */
const EVIDENCE_RETENTION_POLICY: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.evidenceRetentionPolicy,
  description: 'Retention periods evidence and documents may be held under.',
  seed: catalogValues([
    ['Short', 'Short — 1 year', { retentionMonths: 12 }],
    ['Standard', 'Standard — 3 years', { retentionMonths: 36 }],
    ['Extended', 'Extended — 7 years', { retentionMonths: 84 }],
    ['Permanent', 'Permanent', { retentionMonths: 0, permanent: true }],
  ]),
};

// =================================================================================================
// Phase 6 — Document.  No client consumer today; the documentation module renders records as given.
// =================================================================================================

/**
 * Document types and their handling rules.
 *
 * `namingPattern` carries the naming rule as a template over document fields; `versioning` selects
 * the version policy. **No client consumer today.**
 */
const DOCUMENT_TYPE: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.documentType,
  description: 'Document types, their naming, versioning and retention.',
  seed: catalogValues([
    [
      'Policy',
      'Policy',
      {
        namingPattern: 'POL-{code}-v{version}',
        versioning: 'Major',
        retentionPolicy: 'Permanent',
        requiresApproval: true,
      },
    ],
    [
      'Procedure',
      'Procedure',
      {
        namingPattern: 'SOP-{code}-v{version}',
        versioning: 'Major',
        retentionPolicy: 'Extended',
        requiresApproval: true,
      },
    ],
    [
      'WorkInstruction',
      'Work Instruction',
      { namingPattern: 'WI-{code}-v{version}', versioning: 'Minor', retentionPolicy: 'Standard' },
    ],
    [
      'Form',
      'Form',
      { namingPattern: 'FRM-{code}-v{version}', versioning: 'Minor', retentionPolicy: 'Standard' },
    ],
    [
      'Report',
      'Report',
      { namingPattern: 'RPT-{code}-{year}', versioning: 'None', retentionPolicy: 'Extended' },
    ],
  ]),
};

/** Handling classification. `restrictsExternalSharing` is the rule the label stands for. */
const DOCUMENT_CLASSIFICATION: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.documentClassification,
  description: 'Confidentiality classification and its handling rule.',
  seed: catalogValues([
    ['Public', 'Public', { restrictsExternalSharing: false }],
    ['Internal', 'Internal', { restrictsExternalSharing: true }],
    ['Confidential', 'Confidential', { restrictsExternalSharing: true }],
    ['Restricted', 'Restricted', { restrictsExternalSharing: true, requiresNamedAccess: true }],
  ]),
};

/** Version increment policies referenced by document type. */
const DOCUMENT_RETENTION_POLICY: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.documentRetentionPolicy,
  description: 'How a document version number advances on revision.',
  seed: catalogValues([
    ['None', 'No versioning'],
    ['Minor', 'Minor increments (1.1, 1.2)'],
    ['Major', 'Major increments (2.0, 3.0)'],
  ]),
};

// =================================================================================================
// Phase 7 — Dashboard.  Partial consumer: the audit dashboards render fixed KPI sets today.
// =================================================================================================

/**
 * Executive KPIs, their placement and who sees them.
 *
 * `metric` names the value the surface computes; `visibleToRole` restricts a tile; `sortOrder`
 * orders it. This is the shape a role-based layout needs — the layout is the set of cards a role can
 * see, in order — without a per-role layout document.
 *
 * **Partial consumer.** The audit dashboards render fixed tile sets; migrating them is the follow-up
 * this catalog exists for, and is recorded as debt rather than claimed.
 */
const EXECUTIVE_KPI: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.executiveKpi,
  description: 'Executive KPI tiles, their metric, ordering and role visibility.',
  seed: catalogValues([
    ['ComplianceScore', 'Compliance Score', { metric: 'complianceScore', format: 'percent' }],
    ['OpenFindings', 'Open Findings', { metric: 'openFindings', format: 'count' }],
    ['OverdueCapas', 'Overdue CAPAs', { metric: 'overdueCapas', format: 'count', tone: 'danger' }],
    ['AuditsThisQuarter', 'Audits This Quarter', { metric: 'auditsThisQuarter', format: 'count' }],
    ['EvidenceCoverage', 'Evidence Coverage', { metric: 'evidenceCoverage', format: 'percent' }],
    [
      'SlaBreaches',
      'SLA Breaches',
      { metric: 'slaBreaches', format: 'count', tone: 'danger', visibleToRole: 'ORG_ADMIN' },
    ],
  ]),
};

/** Dashboard cards and widgets, with the same visibility and ordering model as KPIs. */
const DASHBOARD_CARD: ConfigurationCatalog = {
  code: ADMINISTRATION_CATEGORY.dashboardCard,
  description: 'Dashboard cards and widgets, their kind, ordering and role visibility.',
  seed: catalogValues([
    ['ComplianceTrend', 'Compliance Trend', { widget: 'lineChart', span: 2 }],
    ['RiskDistribution', 'Risk Distribution', { widget: 'donutChart', span: 1 }],
    ['ChapterBreakdown', 'Chapter Breakdown', { widget: 'barChart', span: 2 }],
    ['UpcomingAudits', 'Upcoming Audits', { widget: 'list', span: 1 }],
    ['ActivityStream', 'Activity Stream', { widget: 'timeline', span: 1 }],
    ['HighRiskChapters', 'High-Risk Chapters', { widget: 'list', span: 1, tone: 'danger' }],
  ]),
};

/** Every Administration configuration domain, in the order the Configuration Center lists them. */
export const ADMINISTRATION_CONFIGURATION_CATALOGS: readonly ConfigurationCatalog[] = [
  RISK_LIKELIHOOD,
  RISK_IMPACT,
  RISK_CATEGORY,
  RISK_ESCALATION_THRESHOLD,
  APPROVAL_LEVEL,
  APPROVAL_OUTCOME,
  NOTIFICATION_CATEGORY,
  NOTIFICATION_CHANNEL,
  NOTIFICATION_PRIORITY,
  SLA_POLICY,
  SLA_WARNING_THRESHOLD,
  BUSINESS_CALENDAR,
  EVIDENCE_CATEGORY,
  EVIDENCE_TYPE,
  EVIDENCE_RETENTION_POLICY,
  DOCUMENT_TYPE,
  DOCUMENT_CLASSIFICATION,
  DOCUMENT_RETENTION_POLICY,
  EXECUTIVE_KPI,
  DASHBOARD_CARD,
];

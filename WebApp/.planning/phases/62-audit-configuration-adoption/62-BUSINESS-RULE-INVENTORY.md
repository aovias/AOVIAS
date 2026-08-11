# Business Rule Inventory — Audit Workspace

Generated from `AUDIT_CONFIGURATION_CATALOGS`. Every row below is a value an administrator can
rename, reorder, deactivate or re-attribute without an Angular change.

**26 categories · 130 values**

## AuditType

Kinds of audit the platform records, schedules and executes.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `InternalAudit` | Internal | `selectableWhenScheduling`=true, `selectableWhenStartingSession`=true |
| 20 | `DepartmentAudit` | Department | `selectableWhenScheduling`=true |
| 30 | `FollowUpAudit` | Follow-up | `selectableWhenScheduling`=true |
| 40 | `MockAudit` | Mock NABH | `selectableWhenScheduling`=true, `selectableWhenStartingSession`=true |
| 50 | `FocusedAudit` | Focused | `selectableWhenScheduling`=true, `selectableWhenStartingSession`=true |
| 60 | `Surveillance` | Surveillance | `selectableWhenScheduling`=true, `selectableWhenStartingSession`=true, `surveillance`=true |
| 70 | `ConsultantReview` | Consultant Review | `selectableWhenScheduling`=false, `selectableWhenStartingSession`=true, `external`=true |
| 80 | `NabhPreAssessment` | NABH Pre-Assessment | `selectableWhenScheduling`=false, `selectableWhenStartingSession`=true, `external`=true |
| 90 | `NabhFinalAssessment` | NABH Final Assessment | `selectableWhenScheduling`=false, `selectableWhenStartingSession`=true, `external`=true |

## AuditRecurrence

Repeat cadence offered when scheduling an audit programme.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `none` | None | `occurrences`=1, `intervalMonths`=0 |
| 20 | `monthly` | Monthly ×3 | `occurrences`=3, `intervalMonths`=1 |
| 30 | `quarterly` | Quarterly ×4 | `occurrences`=4, `intervalMonths`=3 |

## AuditSessionStatus

Lifecycle states of an audit execution session.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Planned` | Planned | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 20 | `InProgress` | In Progress | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 30 | `Review` | Review | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 40 | `DraftReport` | Draft Report | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 50 | `Submitted` | Submitted | `lifecycleStep`=true, `selectableWhenFiltering`=true, `terminal`=true |
| 60 | `Closed` | Closed | `lifecycleStep`=true, `selectableWhenFiltering`=true, `terminal`=true |

## AuditSessionResult

Compliance verdict recorded when an audit session closes.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `NotDetermined` | Not Determined | — |
| 20 | `Compliant` | Compliant | — |
| 30 | `ConditionallyCompliant` | Conditionally Compliant | — |
| 40 | `PartiallyCompliant` | Partially Compliant | — |
| 50 | `NonCompliant` | Non-Compliant | — |

## ScheduledAuditStatus

States of an audit on the schedule, before and after execution.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Scheduled` | Scheduled | — |
| 20 | `InProgress` | In Progress | — |
| 30 | `Completed` | Completed | `terminal`=true |
| 40 | `Closed` | Closed | `terminal`=true |
| 50 | `Cancelled` | Cancelled | — |

## ObservationType

Kinds of observation recorded during an audit session.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `PositivePractice` | Positive Practice | — |
| 20 | `Suggestion` | Suggestion | — |
| 30 | `MinorObservation` | Minor Observation | — |
| 40 | `Strength` | Strength | — |
| 50 | `Opportunity` | Opportunity | — |
| 60 | `GeneralComment` | General Comment | — |

## ReportPeriod

Time windows offered when generating audit reports.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `programYear` | Current programme year | — |
| 20 | `quarter` | Last 3 months | — |
| 30 | `month` | Last 30 days | — |

## FindingStatus

Lifecycle states a finding moves through.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Draft` | Draft | `lifecycleStep`=true, `detailStep`=true, `selectableWhenFiltering`=true, `selectableWhenTransitioning`=true, `selectableWhenCreating`=true |
| 20 | `Open` | Open | `lifecycleStep`=true, `detailStep`=true, `selectableWhenFiltering`=true, `selectableWhenTransitioning`=true, `selectableWhenCreating`=true |
| 30 | `Reviewed` | Reviewed | `lifecycleStep`=true, `detailStep`=true, `selectableWhenFiltering`=true, `selectableWhenTransitioning`=true |
| 40 | `Accepted` | Accepted | `lifecycleStep`=true, `detailStep`=true, `selectableWhenFiltering`=true, `selectableWhenTransitioning`=true |
| 50 | `Promoted` | Promoted | `detailStep`=true, `persisted`=false |
| 60 | `Closed` | Closed | `lifecycleStep`=true, `detailStep`=true, `selectableWhenFiltering`=true, `selectableWhenTransitioning`=true |
| 70 | `InProgress` | In Progress | — |
| 80 | `Verified` | Verified | — |

## FindingSource

Origin of a finding, as raised.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `InternalAudit` | Internal Audit | `ordinal`=1 |
| 20 | `ExternalAudit` | External Audit | `ordinal`=2 |
| 30 | `ConsultantAssessment` | Consultant Assessment | `ordinal`=4 |
| 40 | `NabhAssessment` | NABH Assessment | `ordinal`=5 |
| 50 | `ManagementReview` | Management Review | `ordinal`=6 |
| 60 | `DepartmentAssessment` | Department Assessment | `ordinal`=7 |
| 70 | `IncidentReview` | Incident Investigation | `ordinal`=8 |
| 80 | `ComplianceTeam` | Compliance Team | `ordinal`=3 |

## FindingClassification

Category of finding raised during audit.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `AuditFinding` | Audit Finding | — |
| 20 | `Observation` | Observation | — |
| 30 | `NonConformity` | Non-Conformity | — |
| 40 | `OpportunityForImprovement` | Opportunity For Improvement | `shortLabel`=OFI |
| 50 | `GoodPractice` | Good Practice | — |

## FindingSeverity

How serious a finding is.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Critical` | Critical | `badgeToken`=critical |
| 20 | `Major` | Major | `badgeToken`=major |
| 30 | `Moderate` | Moderate | `badgeToken`=moderate |
| 40 | `Minor` | Minor | `badgeToken`=minor |
| 50 | `Informational` | Informational | `badgeToken`=informational |

## FindingAttachmentType

Kinds of evidence attachable to a finding.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Document` | Document | — |
| 20 | `Image` | Image | — |
| 30 | `AuditNote` | Audit Note | — |
| 40 | `Report` | Report | — |
| 50 | `Screenshot` | Screenshot | — |

## FindingQuickFilter

Saved filters offered as chips on the findings workbench.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `needsReview` | Needs Review | — |
| 20 | `readyForPromotion` | Ready For Promotion | — |
| 30 | `recentlyAdded` | Recently Added | — |
| 40 | `critical` | Critical | — |

## PromotionReadiness

Readiness of a finding for promotion to a deficiency.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Not Eligible` | Not Eligible | — |
| 20 | `Awaiting Acceptance` | Awaiting Acceptance | — |
| 30 | `Eligible` | Eligible | — |
| 40 | `Promoted` | Promoted | — |

## DeficiencyStatus

Lifecycle states a deficiency moves through.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Identified` | Identified | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 20 | `Assigned` | Assigned | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 30 | `InProgress` | In Progress | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 40 | `EvidenceSubmitted` | Awaiting Evidence | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 50 | `ReviewPending` | Verification Pending | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 60 | `Verified` | Verified | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 70 | `Closed` | Closed | `lifecycleStep`=true, `selectableWhenFiltering`=true |
| 80 | `Cancelled` | Cancelled | — |

## DeficiencySeverity

How serious a deficiency is.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Critical` | Critical | `badgeToken`=critical |
| 20 | `Major` | Major | `badgeToken`=major |
| 30 | `Moderate` | Moderate | `badgeToken`=moderate |
| 40 | `Minor` | Minor | `badgeToken`=minor |

## DeficiencyGrouping

Ways the CAPA workspace may group its rows.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `severity` | Group by Severity | `field`=severity, `labelFromCategory`=DeficiencySeverity |
| 20 | `stage` | Group by Stage | `field`=maturityLevel, `labelFromCategory`=MaturityStage |
| 30 | `status` | Group by Status | `field`=status, `labelFromCategory`=DeficiencyStatus |

## RiskLevel

Assessed risk rating of a deficiency.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Low` | Low | `badgeClass`=risk--low |
| 20 | `Medium` | Medium | `badgeClass`=risk--medium |
| 30 | `High` | High | `badgeClass`=risk--high |
| 40 | `Critical` | Critical | `badgeClass`=risk--critical |

## RootCauseCategory

Root cause taxonomy for deficiency analysis.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `PolicyMissing` | Policy Missing | — |
| 20 | `EvidenceMissing` | Evidence Missing | — |
| 30 | `TrainingMissing` | Training Missing | — |
| 40 | `ProcessFailure` | Process Failure | — |
| 50 | `DocumentationIssue` | Documentation Issue | — |
| 60 | `VerificationFailure` | Verification Failure | — |
| 70 | `Other` | Other | — |

## CapaActionStatus

Progress of a corrective or preventive action.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `NotStarted` | Not Started | — |
| 20 | `InProgress` | In Progress | — |
| 30 | `Completed` | Completed | — |

## EffectivenessReviewStatus

Outcome of a CAPA effectiveness review.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `NotRequired` | Not Required | — |
| 20 | `Pending` | Pending | — |
| 30 | `Scheduled` | Scheduled | — |
| 40 | `Effective` | Effective | — |
| 50 | `NotEffective` | Not Effective | — |

## EffectivenessReviewSchedule

Offsets at which a CAPA effectiveness review may be scheduled.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `30` | 30 Day Review | `days`=30 |
| 20 | `60` | 60 Day Review | `days`=60 |
| 30 | `90` | 90 Day Review | `days`=90 |

## MaturityStage

Accreditation maturity stages a deficiency may block.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `Core` | Core | — |
| 20 | `Commitment` | Commitment | — |
| 30 | `Achievement` | Achievement | — |
| 40 | `Excellence` | Excellence | — |

## Department

Departments findings and deficiencies are attributed to.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `hr` | HR | — |
| 20 | `security` | Security | — |
| 30 | `clinical` | Clinical | — |
| 40 | `admin` | Admin | — |

## SlaStatus

States of a deficiency remediation SLA clock.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `OnTrack` | On Track | `badgeClass`=sla sla--ontrack |
| 20 | `Warning` | Warning | `badgeClass`=sla sla--warning |
| 30 | `AtRisk` | At Risk | `badgeClass`=sla sla--atrisk |
| 40 | `Breached` | Breached | `badgeClass`=sla sla--breached |
| 50 | `Closed` | Closed | `badgeClass`=sla sla--closed, `fallback`=true |

## AssignmentConfidence

Confidence of an automated assignment suggestion.

| Order | Code | Label | Attributes |
| --- | --- | --- | --- |
| 10 | `None` | None | `badgeClass`=conf conf--none, `fallback`=true |
| 20 | `Low` | Low | `badgeClass`=conf conf--low |
| 30 | `Medium` | Medium | `badgeClass`=conf conf--medium |
| 40 | `High` | High | `badgeClass`=conf conf--high |

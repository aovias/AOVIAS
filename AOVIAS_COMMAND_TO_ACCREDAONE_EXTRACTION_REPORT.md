# AOVIAS Command → AccredaOne Knowledge and Journey Extraction

**Status:** Read-only investigation. No source, configuration, database, or Git state was changed.

**Date:** 31 October 2026

**Method:** Static inspection of source, seeds, tests, and product documents in `V:\DevelopmentRepo\AOVIAS`. One read-only aggregation of CSV column values (counts and labels only). The application was not run. The test suite was not re-executed. Database contents were not queried, so seed SQL describes what *would* be loaded, not what a given environment currently contains.

**Verification language used below**

| Label | Meaning in this report |
| --- | --- |
| IMPLEMENTED AND VERIFIED | Behaviour is in code, and a test in the repository asserts that behaviour. Tests were read, not re-run. |
| IMPLEMENTED — NOT VERIFIED | Behaviour is in code. No dedicated test was found, or only URL/wiring tests exist. |
| PARTIALLY IMPLEMENTED | A real slice exists; a material part is stubbed, split, or not wired into the user path. |
| DOCUMENTED / DESIGNED ONLY | Described in docs or planning, without a mounted implementation. |
| NOT FOUND | No implementation or design artifact located. |
| DEPRECATED / REPLACED | Present, with a successor called out in code. |

Protected NABH requirement sentences are not reproduced. Identifiers, chapter codes, programme names, and structural field names are included for traceability.

---

## 1. Executive summary

AOVIAS Command has already solved the **shape** of an institutional NABH operating system: framework → chapter → standard → objective element, a hospital journey, evidence attachment, ownership, findings, deficiency CAPA, and readiness dashboards. That execution model should stay in Command.

AccredaOne cannot treat the current Knowledge Base as an authoritative accreditation library. The richest content pipeline is internally generated:

- **503 of 503** objective-element titles and descriptions in `ObjectiveElements.csv` are synthetic placeholders (`Objective Element A/B/…`, `Objective element … for {code} requirement.`).
- Explanation, why-it-matters, how-to-comply, evidence, and audit-checklist text are **templates** in `knowledge-item-templates.json`, with parent standard titles interpolated. Those standard titles are the main rights-sensitive strings. Their official-versus-paraphrase status is **unknown**.
- Every objective element in that CSV is evidence type **Document** and frequency **As Applicable**. Interview and observation branches in the template are unused.
- Ten department labels pair one-to-one with ten role labels. That is a lookup, not a validated applicability model.
- The chapter CSV is parented to the **Entry Level** framework id, then `Generate-NabhSeed.ps1` republishes the tree as **NABH Full Accreditation (Hospital), 6th Edition**.
- The operational Compliance catalog does **not** load that 503-element tree. Full 6E in ComplianceService is a clone of the Entry Level master-data framework, whose checked-in JSON is a **5-chapter, 5-element sample** with a placeholder checksum.
- `FrameworkClassifications` in the Knowledge Base seed stores **chapter names**, not Core / Commitment / Achievement / Excellence. Wizard maturity bands are a later proportional SQL split (~40/30/20/10), and the seed README says official tiers must be confirmed in the PDF.
- Accreditation thresholds (80 percent, average score ≥ 4, Core ≥ 4, at most one score ≤ 2) are an **internal workspace contract** in `NabhAccreditationEvaluator`. They are product rules. They are not evidenced here as official NABH scoring methodology.
- There is **no source, citation, licence, or edition-provenance field** on knowledge entities. No NABH usage rights are documented in the repository.
- `AOVIAS.AccredaOne` is a **scaffold only**. It has no runtime.

**What AccredaOne can learn:** the hierarchy, the vocabulary (chapter, standard, objective element, evidence type, maturity band, applicability, gap severity), and the learning-shaped steps already present inside the objective-element wizard (Understanding → Compliance Guidance → Evidence expectations → Audit checklist → Review). Those steps are an execution wizard, not a public learning product, and they currently render non-authoritative text.

**What must not transfer:** hospital enrollment, obligation tracking, evidence repositories, task assignment, audit sessions, findings, CAPA, institutional dashboards, and any score presented as an accreditation determination.

**Product Owner decision required before any content reuse:** rights and provenance. Until that is resolved, AccredaOne should treat Command knowledge text as a **schema and journey prototype**, not as publishable accreditation content.

---

## 2. Repository areas inspected

| Area | Path | What was used for |
| --- | --- | --- |
| Command compliance domain | `AOVIAS.Command/ComplaincePlatform/ComplianceService/Domain/` | Operational entities |
| Journey, scoring, enrollment | `…/ComplianceService/Application/Journey/`, `…/Application/Services/` | Readiness and workspace rules |
| Master data | `…/Compliance.MasterData/` | Entry Level JSON seed |
| Knowledge Base | `…/KnowledgeBase/Domain/`, `…/Application/`, `…/Controllers/` | Interpretive content and assessments |
| Seeds | `…/seed-data/` | Provenance, counts, generation |
| Gateway | `…/Gateway/Controllers/` | External API surface |
| WebApp journey | `AOVIAS.Command/WebApp/src/app/features/journey/` | End-user OE experience |
| Related WebApp areas | `features/onboarding`, `evidence`, `audit`, `gap-analysis`, `documentation`, `dashboard`, `training`, `compliance-graph` | Adjacent journeys |
| Tests | `ComplianceService.Tests/`, `Knowledgebase.Tests/`, `tests/Gateway.UnitTests/`, WebApp `*.spec.ts` | What is asserted |
| Architecture | `docs/Architecture/01-Executive-Summary.md`, `docs/Architecture/ADR-014-Framework-Presentation-vs-Journey-Identity.md` | Product intent |
| AccredaOne | `AOVIAS.AccredaOne/README.md` | Current product boundary |
| Prior assessment | `ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` | Compared, not treated as current fact |

Folder name `ComplaincePlatform` is the repository spelling.

Not inspected as running systems: SQL Server data, deployed environments, the official NABH PDF (linked from the seed README, not stored in the repo), or the external CSV origin `v:\DevelopmentRepo\New folder\`.

---

## 3. NABH domain inventory

Command implements accreditation concepts in **three stores** that are not one catalog.

| Store | Role | NABH content actually present in source |
| --- | --- | --- |
| ComplianceService (`ComplianceDbContext`) | Hospital execution: journeys, scores, evidence, findings, CAPA, documents | Generic model. Checked-in NABH seed is Entry Level sample JSON, optionally cloned to a second framework id. |
| KnowledgeBase (`KnowledgeBaseContext`) | Hierarchy, guidance text, wizard questions, KB assessments | Generated 6th Edition hospital seed: 10 chapters, 100 standards, 503 objective elements, templated guidance. |
| Compliance.MasterData | JSON seed contract | `nabh-entry-2024.json`: 5 chapters, 5 objective elements. Manifest authority `QCI`, checksum `sha256-placeholder`. |

Cross-store link: `OrganizationComplianceObligation.ComplianceNodeId` is a nullable Guid with **no foreign key** to Knowledge Base.

There is no `Programme` entity. Pathways are framework name + version + `PlatformId`.

### 3.1 Frameworks and versions

| Concept | Symbol | Path | Table | Status |
| --- | --- | --- | --- | --- |
| Operational framework | `ComplianceFramework` | `ComplianceService/Domain/ComplianceFramework.cs` | `ComplianceFrameworks` | IMPLEMENTED — NOT VERIFIED as a full NABH edition |
| Per-framework rules | `FrameworkConfiguration` | `ComplianceService/Domain/FrameworkConfiguration.cs` | `FrameworkConfigurations` | IMPLEMENTED — NOT VERIFIED |
| Knowledge framework | `Framework` | `KnowledgeBase/Domain/Entities/Framework.cs` | `Frameworks` | IMPLEMENTED — NOT VERIFIED |
| Entry seed manifest | manifest keys `framework`, `version`, `authority`, `publishedOn`, `checksum` | `Compliance.MasterData/Data/NABH/EntryLevel/manifest.json` | n/a | PARTIALLY IMPLEMENTED (placeholder checksum) |
| Full 6E clone | `NabhFull6eCatalogReplicationSeeder` | `ComplianceService/Application/Seeders/NabhFull6eCatalogReplicationSeeder.cs` | Compliance tables | IMPLEMENTED — NOT VERIFIED. Clones `CP00000002` → `CP00000001` |
| Programme name list | CSV only | `seed-data/source/MasterFrameworks.csv` | not loaded except via KB generator for one row | DOCUMENTED IN CSV ONLY |

CSV programme **names** (labels only; not seeded as Knowledge Base trees except the hospital 6th Edition row):

| PlatformId | Name in CSV | Version in CSV |
| --- | --- | --- |
| CP00000001 | NABH Full Accreditation (Hospital) | 6th Edition |
| CP00000002 | NABH Entry Level Accreditation (Hospital) | Entry Level v1.0 |
| CP00000003 | NABH Small Healthcare Organization (SHCO) | SHCO v2.0 |
| CP00000004 | NABH Emergency Department Accreditation | v1.0 |
| CP00000005 | NABH Nursing Excellence Certification | v1.0 |
| CP00000006 | NABH Blood Bank Accreditation | v1.0 |
| CP00000007 | NABH Medical Laboratory Accreditation | v1.0 |
| CP00000008 | NABH Imaging Services Accreditation | v1.0 |
| CP00000009 | NABH Dialysis Centre Accreditation | v1.0 |
| CP00000010 | NABH AYUSH Hospital Accreditation | v1.0 |
| CP00000011 | NABH Dental Healthcare Accreditation | v1.0 |

JCI, NABL, and ISO appear in architecture futures and some enums. No equivalent seeded catalogs were found.

### 3.2 Chapters, standards, objective elements

**Operational hierarchy**

```text
ComplianceFramework
  └── ComplianceChapter          ComplianceService/Domain/ComplianceChapter.cs
        └── ComplianceStandard   ComplianceService/Domain/ComplianceStandard.cs
              └── ObjectiveElement
```

`ObjectiveElement` (`ComplianceService/Domain/ObjectiveElement.cs`) fields that matter for knowledge design: `Code`, `Title`, `Description`, `Classification` (`Core`, `Commitment`, `Achievement`, `Excellence`), `IsMandatory`, `IsApplicable`, `IsDocumentationRequired`, `Department`, `ResponsibleRole`, `Frequency`, `EvidenceCategory`, `Weight`.

Entry Level JSON does not model a standard layer. Persistence creates requirements by flattening them onto the objective element (`IMasterDataPersistence.CreateRequirementAsync`). Master seed often uses a per-chapter standard code `DEFAULT` when the JSON has no standards. That path is separate from the 100-standard Knowledge Base tree.

**Knowledge hierarchy**

```text
Framework
  └── ComplianceNode (NodeType Chapter | Standard | ObjectiveElement)
        ├── RequirementDetail   (seeded on standards, not on objective elements)
        ├── KnowledgeItem       (seeded on objective elements)
        └── WizardQuestion      (one derived row per objective element)
```

Symbols: `ComplianceNode`, `RequirementDetail`, `KnowledgeItem`, `WizardQuestion` under `KnowledgeBase/Domain/Entities/`.

Chapter codes in the 6E seed (identification only): AAC, COP, MOM, PRE, IPC, PSQ, ROM, FMS, HRM, IMS.

Entry Level JSON chapter codes are a different set: PRE, ACC, ICC, HRM, FMS, each with a single `*.1` objective element. `ACC` versus `AAC`, and `ICC` versus `IPC`, are not the same coding scheme.

### 3.3 Interpretations, applicability, evidence expectations

| Concept | Where | What it actually contains | Authority |
| --- | --- | --- | --- |
| Intent | `RequirementDetail.Intent` | Generator copies the parent **standard title**. `MeasurableElements` is stored empty. | Unknown / imported title, not an authored interpretation |
| Explanation | `KnowledgeItem.Explanation` | `{Code}: {Title}. {Description}` from the synthetic OE row | Synthetic |
| Why it matters, how to comply, audit checklist | `KnowledgeItem` + `knowledge-item-templates.json` | Fixed templates plus chapter, department, role, and **standard title** placeholders | Internally authored template; may embed standard titles |
| Evidence required | `KnowledgeItem.EvidenceRequired` | Intro plus a body chosen by evidence type. CSV evidence type is **Document for all 503 rows** | Generic template |
| Applicability | `ObjectiveElement.IsApplicable`, `ObjectiveElementScore.IsApplicable` / `NaReason` | Catalog default plus per-journey not-applicable | Operational, not a knowledge rule |
| Department scope | OE `Department`; `RequirementDetail.ApplicableDepartments` | Ten free-text buckets in the CSV | Illustrative |
| Role | OE `ResponsibleRole` | Ten roles, counts identical to the ten departments | Illustrative |
| Evidence category | OE `EvidenceCategory`; `EvidenceTypeCatalog` | Operational hint versus uploaded artifact type | Mixed |
| Source reference | No entity | Copilot `SourceReferences` is rule traceability, not a standard citation | Not a provenance model |

### 3.4 Other domain concepts

| Concept | Primary symbol | Path | Notes |
| --- | --- | --- | --- |
| Hospital × framework lifecycle | `ComplianceJourney` | `ComplianceService/Domain/ComplianceJourney.cs` | Operational mode, scores, certificate fields |
| Lifecycle phase | `OperationalMode` | `Shared.Infrastructure/Domain/Enums/OperationalMode.cs` | SelfAssessment, InternalAudit, FinalAudit, Surveillance, ReAccreditation |
| Which bands are visible | `OperationalModeClassificationPolicy` | `ComplianceService/Domain/Policies/OperationalModeClassificationPolicy.cs` | Product policy. Tested. |
| Accreditation eligibility | `NabhAccreditationEvaluator` | `ComplianceService/Application/Journey/NabhAccreditationEvaluator.cs` | Internal contract. Tested. |
| Finding-based score | `DeterministicNabhScoringService` | `ComplianceService/Application/Services/DeterministicNabhScoringService.cs` | Separate from OE 1–5 scores. No dedicated unit test found. |
| Canonical OE score | `ComplianceScoreService`, `AggregationService` | `ComplianceService/Application/Services/ComplianceScoreService.cs`, `…/Aggregation/AggregationService.cs` | Parity test exists |
| Obligation | `OrganizationComplianceObligation` | `ComplianceService/Domain/OrganizationComplianceObligation.cs` | Hospital implementation state |
| Uploaded evidence | `Evidence`, `ObjectiveElementEvidence` | `ComplianceService/Domain/` | Institutional files |
| Controlled documents | `ControlledDocument` | `ComplianceService/Domain/Documentation/ControlledDocument.cs` | Policy, SOP, form, procedure, checklist |
| Gap (computed) | `GapResult`, detectors | `ComplianceService/Domain/GapResult.cs` | Not a persisted knowledge gap |
| Gap (KB assessment) | `GapClassifier`, `GapAnalysisReport` | `KnowledgeBase/Application/GapAnalysis/GapClassifier.cs` | Score bands Critical / Major / Minor / Compliant |
| Finding | `Finding`, `ComplianceAuditFinding` | `ComplianceService/Domain/Finding.cs` | TPH `Findings` |
| CAPA | `ComplianceDeficiency` | `ComplianceService/Domain/ComplianceDeficiency.cs` | SSOT. Legacy `Capa` is deprecated |
| OE remediation | `ActionPlan` | `ComplianceService/Domain/ActionPlan.cs` | Lighter than deficiency CAPA |
| Ownership | `ObjectiveElementOwnership`, `DepartmentOwnership` | `ComplianceService/Domain/` | Users and departments |
| Audit session | `AuditSession`, `AuditObservation`, `ScheduledAudit` | `ComplianceService/Domain/Auditor/` | Includes mock and NABH session types |
| Maturity unlock | `OrganizationMaturityStageState` | `ComplianceService/Domain/OrganizationMaturityStageState.cs` | Institutional progression |
| NABH transition policy | `NabhJourneyPolicy` | `ComplianceService/Domain/NabhJourneyPolicy.cs` | `CanTransition` returns true. Stub. |
| Staff training | WebApp `features/training` | programmes, sessions, matrix | Workforce records, not accreditation lessons |
| Compliance graph | `EnterpriseComplianceGraphService` | `ComplianceService/Application/Services/EnterpriseComplianceGraphService.cs` | Documents and cross-references, not a learning graph |

---

## 4. Reconstructed end-to-end journey

The path that is actually wired:

```text
/onboarding
  → POST enroll + create ComplianceJourney
  → hospital/journey selector (ADR-014: UI speaks framework, APIs need journey id and framework id)
  → /app/journey/execution          chapter and standard navigation
  → /app/journey/execution/Progress/:oeGuid
        Understanding → Compliance Guidance → Evidence → Audit Checklist → Review & Submit
  → deficiencies, findings, CAPA, audit workspace, dashboards, readiness reports
```

Step labels: `OE_WIZARD_STEP_LABELS` in `WebApp/src/app/features/journey/models/oe-execution-wizard.model.ts`.

### Stage 1 — Hospital onboarding and pathway selection

| | |
| --- | --- |
| User objective | Create an organisation and hospital, choose a framework, capture an initial assessment, invite a team |
| Role | Copy targets organisation admin and quality leads. Routes use auth and onboarding guards (`app.routes.ts`, `features/onboarding/routes.ts`) |
| User action | Complete `AiSetupAssistantComponent` steps, including `FrameworkSelectorStepComponent` and `AssessmentStepComponent` |
| Information | Framework choices and an onboarding questionnaire |
| Domain objects | `ComplianceJourney`, `Regulation`, `WizardOnboardingAssessment` |
| Backend | `OnboardWizardCommandHandler`; `POST …/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll` (`EnrollHospitalInFrameworkCommandHandler`); `POST …/journeys` |
| Frontend | `/onboarding` |
| Dependencies | Identity, organisation directory, framework catalog |
| Status | IMPLEMENTED — NOT VERIFIED as a full wizard. Enrollment access is tested in `EnrollHospitalAccessTests`. Composer tests cover DTO shape, not the hospital journey end to end. |
| Limitations | Pathway list depends on whatever frameworks exist in Compliance data. The 11 CSV programme names are not a seeded multi-pathway catalog. `RegulationsController` enrollment is deprecated in favour of `FrameworkController`. |

### Stage 2 — Understanding accreditation structure

| | |
| --- | --- |
| User objective | See chapters, standards, and objective elements, and filter by maturity stage |
| Role | Authenticated hospital user. Auditor chip when roles include regulator-style ids (`journey-feature-shell.component.ts`) |
| User action | Open execution or maturity launchpad; filter by stage |
| Information | Chapter and standard panels, stage strip |
| Domain objects | `ComplianceChapter`, `ComplianceStandard`, `ObjectiveElement`, maturity classifications |
| Backend | `GET /api/v1/journeys/{journeyId}/chapters` and chapter standards. `StageUnlockingService` |
| Frontend | `/app/journey/execution`, `/app/journey/execution/stage/:stage`, `/app/journey/maturity`. Catalog admin is `/app/frameworks` (`FrameworkNavigatorComponent`), which is not the learner view |
| Status | IMPLEMENTED — NOT VERIFIED for the screen. Stage unlock criteria have tests (`StageUnlockCriteriaEvaluatorTests`). |
| Limitations | If Full 6E was cloned from the Entry Level JSON, the hospital sees about five sample elements, not the 503-node Knowledge Base tree. The two hierarchies are not the same product. |

### Stage 3 — Reviewing applicable requirements

| | |
| --- | --- |
| User objective | See which objective elements are in scope for the current operational mode |
| User action | Change operational mode; browse the filtered tree |
| Domain objects | `OperationalMode`, `ObjectiveElementClassification` |
| Backend | `GET/PATCH …/journeys/{journeyId}/session`. Policy: `OperationalModeClassificationPolicy` |
| Frontend | `/app/journey/execution`, `/app/journey/operational` (`LifecycleModeSwitchComponent`) |
| Status | IMPLEMENTED AND VERIFIED for the classification policy (`OperationalModeClassificationPolicyTests`, `ComplianceJourneyOperationalModeTests`). UI binding is IMPLEMENTED — NOT VERIFIED. |
| Rule (product, not cited as official NABH) | Workspace: self-assessment and re-accreditation show all four bands; internal audit and surveillance omit Excellence; final audit shows Core and Commitment. Decision rules are narrower than the workspace for several modes. |
| Limitations | Bands are only meaningful if `ObjectiveElement.Classification` was loaded from an authoritative source. The Knowledge Base wizard classifier does not write this operational field. |

### Stage 4 — Understanding an objective element

| | |
| --- | --- |
| User objective | Read what the element means before doing compliance work |
| User action | Open `/app/journey/execution/Progress/:oeGuid` |
| Information | Wizard step **Understanding**: explanation, why it matters, risk/impact/tags, requirement intent, departments, mandatory and documentation flags. Fallback copy exists when Knowledge Base text is missing. **Smart assist** is placeholder text: summary will appear when AI assist is enabled. |
| Domain objects | `KnowledgeItem`, `RequirementDetail`, `ObjectiveElement` |
| Backend | Journey OE detail plus Knowledge Base node/knowledge by code (`KnowledgeBaseDataService`) |
| Frontend | `OeExecutionWizardComponent` step 0. Mapper: `knowledge-base-oe-wizard.mapper.ts` |
| Status | IMPLEMENTED — NOT VERIFIED. Service spec checks HTTP mapping, not content quality. |
| Limitations | Rendered explanation is the synthetic OE string unless a human has replaced Knowledge Items in a database. `RequirementDetail` in the seed hangs off the **standard**, and measurable elements are empty. `OeDetailDrawerComponent` exists and is not the mounted primary experience. |

### Stage 5 — Preparing policies, procedures, and records

| | |
| --- | --- |
| User objective | Author controlled documents and map them to an objective element |
| User action | Use the documentation workspace |
| Domain objects | `ControlledDocument`, `ControlledDocumentType` (Policy, Sop, Form, Procedure, Checklist, …), versions, reviews, approvals |
| Backend | `DocumentationController` under `api/v{version}/documentation/*` |
| Frontend | `/app/documentation/**` |
| Status | IMPLEMENTED — NOT VERIFIED from this pass (QDMS lifecycle tests exist: `DocumentControlLifecycleTests`) |
| Limitations | Separate from the journey tree. This is institutional document control, not a library of example policies for learners. |

### Stage 6 — Identifying evidence expectations

| | |
| --- | --- |
| User objective | See what proof is expected, then attach files |
| Information | Wizard **Compliance Guidance** shows how-to-comply and evidence-expectation copy. Wizard **Evidence** is an upload workspace. |
| Domain objects | `KnowledgeItem.EvidenceRequired`, `ObjectiveElement.EvidenceCategory`, `Evidence`, `ObjectiveElementEvidence` |
| Backend | Journey evidence routes on `JourneysController`. Library and binder controllers. |
| Frontend | Wizard steps 1–2; `/app/evidence`, review queue, binder |
| Status | PARTIALLY IMPLEMENTED. Upload and verification state machine are implemented (`EvidenceVerificationStateMachineTests`). Expectation **text** is a template. Binder checklist API returns an empty list (`EvidenceBinderQueryService.GetChecklistAsync`). |
| Limitations | CSV evidence type is Document for every row, so learners would see the same document checklist with the code interpolated. |

### Stage 7 — Self-assessment

| | |
| --- | --- |
| User objective | Score the objective element and complete the wizard |
| User action | Audit-checklist ticks, evaluation tone (compliant / partial / non), 1–5 score, review and submit |
| Domain objects | `ObjectiveElementScore`, `ObjectiveJourney`, wizard payload |
| Backend | `GET/PUT …/oe-execution-wizard`, `POST …/complete`. Separate onboarding assessment in Knowledge Base. |
| Frontend | Wizard steps **Audit Checklist** and **Review & Submit** |
| Status | IMPLEMENTED — NOT VERIFIED for the full wizard. `OeWizardContractTests` cover canonicalization and score normalisation. |
| Limitations | Onboarding assessment and journey scoring are different flows. Client note `KB_SCORE_SCALE_PRODUCT_NOTE` says Knowledge Base `maxScore` is not bound to the wizard slider. Local wizard state can live in session storage before server draft merge. |

### Stage 8 — Identifying gaps

| | |
| --- | --- |
| User objective | See where scores or missing action plans fail internal rules |
| Two implementations | (1) Knowledge Base assessment gap report. (2) Compliance deficiencies and computed gap detectors. |
| KB rules | `AssessmentSummaryCalculator`: overall percent from 1–5 scores; Core failures when classified Core and score &lt; 4; commitment average ≥ 80; more than one low score per standard; score ≤ 3 without an action plan. `GapClassifier`: Critical if core failure, action-plan violation, or score ≤ 1; Major if score = 2 or multiple lows in the standard; Minor if score = 3. |
| Frontend | `/app/gap-analysis/:assessmentId` |
| Status | IMPLEMENTED AND VERIFIED for KB gap POST and classifier-related tests (`KnowledgeApiIntegrationTests`, `GapClassifierTests`, `AssessmentSummaryCalculatorTests`). Journey execution does not deep-link an assessment id automatically. |
| Limitations | Gap severity is an internal scoring heuristic. It is not an official nonconformity class. |

### Stage 9 — Assigning responsibilities

| | |
| --- | --- |
| User objective | Assign an owner, department, and tasks |
| Backend | `OeOwnershipController` `GET/PUT/DELETE /api/v1/hospitals/{hospitalId}/oe-ownership`. Compliance tasks APIs. |
| Frontend | `/app/organization/oe-ownership`; task drawer inside the wizard; `/app/tasks` |
| Status | IMPLEMENTED — NOT VERIFIED (drawer component spec exists) |
| Classification | Command-exclusive execution |

### Stage 10 — Internal assessment or audit

| | |
| --- | --- |
| User objective | Plan sessions, record observations, run simulation |
| Backend | `AuditorController` (`api/v{version}/auditor/*`), `AuditScheduleController`, `AuditFacadeController`. `AuditType` includes internal, mock, NABH pre-assessment, NABH final, surveillance. |
| Frontend | `/app/audit/**`. Legacy `/app/auditor/**` redirects. |
| Role | Some auditor surfaces use `auditSurfaceGuard` and auditor role ids |
| Status | IMPLEMENTED — NOT VERIFIED for the product surface. Session close and finding gates are tested (`AuditSessionCloseGateTests`, `AuditSessionFindingGateTests`). |
| Limitations | `docs/Architecture/01-Executive-Summary.md` still describes the external auditor workspace as planned, about 20 percent design. Treat that maturity claim as documentation, not as a measured audit of the current tree. |

### Stage 11 — Recording findings

| | |
| --- | --- |
| Backend | `AuditFindingsController` and `FindingsController`, including promotion eligibility |
| Frontend | `/app/audit/findings` |
| Domain | `Finding` with `ObjectiveElementId`, severity, `NonConformanceType` (MajorNC, MinorNC, Observation, OFI) |
| Status | IMPLEMENTED — NOT VERIFIED for the UI. `FindingPromotionServiceTests` cover promotion rules. |

### Stage 12 — Corrective action

| | |
| --- | --- |
| Domain | `ComplianceDeficiency` status path: Identified → Assigned → InProgress → EvidenceSubmitted → ReviewPending → Verified → Closed, plus Cancelled. Inline root cause, corrective and preventive action, effectiveness review. |
| Backend | Gateway `DeficienciesController` CAPA patches. Verify role gate blocks self-verify (`CapaVerifyRoleGateTests`). |
| Frontend | `/app/audit/capa` |
| Status | IMPLEMENTED AND VERIFIED (`ComplianceDeficiencyCapaLifecycleTests`, `DeficiencyLifecycleTransitionsTests`) |
| Classification | Command-exclusive |

### Stage 13 — Monitoring progress

| | |
| --- | --- |
| Surfaces | `/app/dashboard`, `/app/journey/maturity`, `/app/journey/operational`, `/app/control-center`, `/app/compliance-maturity`, `/app/reports/readiness`, `/app/analytics` |
| Backend | Journey dashboard; `GET /api/v1/journey/readiness`; `GET /api/v1/reports/nabh-readiness`; v1.1 scoring aggregate |
| Status | PARTIALLY IMPLEMENTED. Eligibility math is IMPLEMENTED AND VERIFIED. `DeterministicNabhScoringService` is a second algorithm without a dedicated test. `RecommendationPanelComponent` and `AccreditationDecisionPanelComponent` exist and are not mounted on the current execution templates. |
| Limitation | A reader can see more than one “readiness” number (OE average, finding-based deterministic score, accreditation eligible flag). They are not the same concept. |

### Stage 14 — Preparing for external assessment

| | |
| --- | --- |
| What exists | Readiness report, audit simulation, journey approve/reject/suspend transitions, mock and NABH session types |
| What does not | NABH-specific lifecycle gates. `NabhJourneyPolicy.CanTransition` returns true for every transition. |
| Status | PARTIALLY IMPLEMENTED |
| Classification | Institutional assurance. A learner-facing “what an assessor looks for” experience is not a separate product surface. The wizard audit checklist is the closest content, and it is template text. |

---

## 5. Requirement and knowledge structure

The structure worth keeping as a **conceptual model**:

```text
Accreditation pathway (framework + edition)
  └── Chapter (stable code, sequence)
        └── Standard (code, statement, weight)
              └── Objective element (code, classification, intent, evidence expectation)
                    └── Learning facets: meaning, why it matters, how to prepare,
                        evidence types, related practice, self-check
```

What the code actually stores is thinner:

| Layer | Intended by field names | Actual 6E seed |
| --- | --- | --- |
| Chapter | Domain area | 10 named chapters. Useful as navigation labels. Rights still need a decision before public display. |
| Standard | Requirement statement | 100 non-empty titles in `Standards.csv`. **Do not treat as licensed text.** Provenance unknown. |
| Objective element | Measurable element | 503 synthetic titles and descriptions. Codes such as `AAC.5.b` are structural ids only. |
| Intent | Why the requirement exists | Copy of the standard title. Measurable elements blank. |
| Knowledge item | Interpretation | Template. Standard title is interpolated into why-it-matters and how-to-comply. |
| Classification | Core / Commitment / Achievement / Excellence | **Not** on `FrameworkClassifications`. That table is filled with chapter names, weight 1, all mandatory (`Generate-NabhSeed.ps1` comment: “FrameworkClassifications (chapters)”). Wizard bands are applied later by `classify-wizard-questions-nabh-6e.sql` using a proportional chapter split. Version 1 of that script put all 503 rows into Core and Commitment; version 2 exists because Achievement and Excellence were zero. |
| Scoring config | Scale | `ScoringConfigs`: fully 5, partial 3, non-compliant 1, method `WeightedAverage`, scale `NABH5Point`. Product configuration. |

`QuestionnaireGenerator` can derive questions from the node graph. `seed-wizard-questions-nabh-6e.sql` materialises 503 questions with a header that says the text is deterministic derivation from existing rows, not hand-authored and not an AI draft. Derivation does not create authority. It copies synthetic OE text and standard titles into `QuestionText`, `HelpText`, `AssessmentGuidance`, and `ExpectedEvidence`.

Knowledge HTTP API is read-oriented: `KnowledgeController` GET by node, by code, and search. No in-app authoring API was found. Population is SQL.

Search (`KnowledgeRepository.SearchAsync`) is a SQL `LIKE` over explanation, tags, how-to-comply, node code, and title, ordered by version, **`Take(10)`**. It is not semantic search and cannot see fields it does not query (`WhyItMatters`, `EvidenceRequired`, `AuditChecklist` are excluded).

---

## 6. Evidence and readiness model

Two different ideas share the word “evidence”.

| Idea | Command implementation | AccredaOne use |
| --- | --- | --- |
| Evidence **expectation** | Template paragraph plus OE `EvidenceCategory`. Binder checklist is an empty stub. | Relevant as a concept: document, interview, observation, record, policy. Current sentences are not OE-specific. |
| Evidence **repository** | `Evidence` file URL, hash, lifecycle, verification, linked by `ObjectiveElementEvidence` | Command-exclusive. A public product must not store a hospital’s proof. |

Readiness is also several mechanisms:

| Mechanism | Inputs | Output | Nature |
| --- | --- | --- | --- |
| `AggregationService` / `ComplianceScoreService` | Latest 1–5 `ObjectiveElementScore`, weights, applicability, operational-mode filter | Compliance percent `(average / 5) * 100` | Institutional score |
| `NabhAccreditationEvaluator` | That aggregate plus Core flags | Eligible or a violation list: overall &lt; 80; chapter or standard average &lt; 4; any applicable Core &lt; 4; more than one applicable score ≤ 2. Not-applicable rows are excluded from the low-score cap. | Internal eligibility contract. Tested. |
| `DeterministicNabhScoringService` | Latest **audit-scoped findings** per OE | Weights Fully 1.0, Partial 0.5, else 0; NA with remarks excluded; mandatory noncompliant fails the standard; chapter pass at ≥ 80 and children passing; separate risk score | Second algorithm. Not the same as the wizard score. |
| `AssessmentSummaryCalculator` | Knowledge Base assessment items | Similar percent, Core, commitment, and action-plan rules | Assessment-session heuristic |
| `GapClassifier` | Those summary sets and the item score | Critical / Major / Minor / Compliant | Advisory-looking labels that currently behave like determinations |
| Maturity stage unlock | Organisation stage state | Progress and unlock | Institutional |

`OperationalModeClassificationPolicy` is the clearest reusable **concept**: preparation (self-assessment) shows a wider set than a final-audit view. AccredaOne could teach that difference. It should not copy the thresholds as official accreditation mathematics without a source review.

---

## 7. Frontend experience inventory

Authenticated shell base is `/app` (`WebApp/src/app/app.routes.ts`).

| Experience | Route | Relevance to a learning product |
| --- | --- | --- |
| Onboarding wizard | `/onboarding` | Pathway selection pattern. Tied to tenant creation. |
| Journey execution map | `/app/journey/execution` | Best structural navigation pattern in the product. |
| OE wizard | `/app/journey/execution/Progress/:oeGuid` | Closest existing learning sequence, embedded in execution. |
| Maturity launchpad | `/app/journey/maturity` | Institutional progress. |
| Operational mode | `/app/journey/operational` | Advanced / auditor view of the same journey. |
| Framework catalog admin | `/app/frameworks` | Administration, not discovery. |
| Gap analysis | `/app/gap-analysis/:assessmentId` | Post-assessment report. Needs an assessment id. |
| Evidence library | `/app/evidence/**` | Institutional. |
| Documentation | `/app/documentation/**` | Institutional document control. |
| Audit, findings, CAPA | `/app/audit/**` | Institutional assurance. |
| Dashboards and reports | `/app/dashboard`, `/app/reports/readiness`, control center | Institutional. |
| Compliance graph | `/app/compliance-graph/**` | Document and entity graph for the hospital. |
| Copilot | `/app/copilot` | Deterministic recommendations for operators. |
| Training | `/app` training routes: programmes, sessions, matrix | Staff training records, not standards education. |
| OE ownership | `/app/organization/oe-ownership` | Institutional RACI. |

Wizard knowledge binding is real: `oe-execution-wizard.component.html` renders `explanation`, `whyItMatters`, `howToComply`, requirement intent, and evidence copy from `OeWizardKbVm`. Empty states fall back to generic sentences such as “Meet documented objective element criteria for this audit phase.” Smart assist is explicitly unimplemented copy.

Accessibility notes from the wizard template: step buttons expose `aria-current`, the main region uses `aria-busy` while knowledge loads, error state uses `role="alert"`, and some labels are screen-reader-only. This investigation did not run a screen reader or keyboard pass. Treat accessibility as partially considered in markup, not verified.

---

## 8. Backend and domain implementation inventory

Client traffic is designed to enter through the Gateway. ComplianceService and KnowledgeBase are separate services. Representative groups:

| Group | Representative routes | Owner |
| --- | --- | --- |
| Catalog and enroll | `GET api/v{version}/frameworks/catalog`, `POST …/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll` | Gateway `FrameworkController`, ComplianceService equivalent |
| Journey workspace | `GET/PATCH api/v{version}/journeys/{journeyId}/session\|dashboard\|chapters\|objective-elements\|oe-execution-wizard` | `JourneysController` |
| Knowledge | `GET api/v{version}/knowledge/node/*`, `search` | `KnowledgeController` |
| Assessments and gaps | `POST/GET api/v{version}/assessments/*`, `gap-analysis` | KnowledgeBase `AssessmentsController` |
| Evidence | Journey OE evidence; `api/v{version}/evidence/*`; `evidence-binder/*` | ComplianceService |
| Documents | `api/v{version}/documentation/*` | `DocumentationController` |
| Tasks and ownership | `compliance-tasks`, `tasks/*`, `oe-ownership` | ComplianceService |
| Findings and CAPA | `findings/*`, `deficiencies/*` | ComplianceService. No public `CapaController`. |
| Audit | `auditor/*`, `audit-schedule`, `audit/generate` | ComplianceService |
| Readiness | `reports/nabh-readiness`, v1.1 `scoring/…/aggregate` | Reporting and `ScoringReadController` |
| Graph | `graph/*` | `ComplianceGraphController` |
| Ask | `POST api/v{version}/ai/ask` | KnowledgeBase `AiController` (uses the same 10-row search) |

Authorization for journey APIs is primarily `ComplianceScope` plus hospital tenancy (`HospitalAccessAuthorizer`). CAPA verification has an extra role gate. There is no learner or anonymous reader role.

Duplicate API surfaces (journey evidence vs v1.1 evidence vs task evidence; Gateway vs service controllers) are an operational concern for Command. They are not knowledge assets.

---

## 9. Existing tests and verification evidence

This session did not run `dotnet test` or the WebApp unit tests. The table is an inventory of tests present in source.

| Behaviour | Test | What it supports |
| --- | --- | --- |
| Accreditation eligibility thresholds and NA exclusion | `ComplianceService.Tests/Journey/NabhAccreditationEvaluatorTests.cs` | Seven facts on the internal contract. Commitment is not the focus of this file. |
| Operational-mode band sets | `OperationalModeClassificationPolicyTests.cs`, `ComplianceJourneyOperationalModeTests.cs` | Domain policy |
| OE score aggregation parity | `ComplianceScoreServiceParityTests.cs` | One canonical OE score path, not the finding-based scorer |
| Finding-based NABH scorer | No dedicated test found | Do not describe `DeterministicNabhScoringService` as verified |
| Wizard score canonicalization | `OeWizardContractTests.cs` | Contract, not content |
| CAPA lifecycle and verify role | `ComplianceDeficiencyCapaLifecycleTests.cs`, `CapaVerifyRoleGateTests.cs`, `DeficiencyLifecycleTransitionsTests.cs` | Execution workflow |
| Evidence verification and types | `EvidenceVerificationStateMachineTests.cs`, `EvidenceTypeCatalogTests.cs` | Upload lifecycle |
| Audit session gates | `AuditSessionCloseGateTests.cs`, `AuditSessionFindingGateTests.cs` | Session rules |
| Finding promotion | `FindingPromotionServiceTests.cs` | Promotion, not survey judgement |
| KB questionnaire, summary, gap class | `Knowledgebase.Tests`: `QuestionnaireGeneratorTests`, `AssessmentSummaryCalculatorTests`, `GapClassifierTests`, `KnowledgeApiIntegrationTests` | Assessment mechanics on whatever nodes the tests build |
| Enrollment access | `EnrollHospitalAccessTests.cs` | Tenancy |
| Journey HTTP paths | `journey-data.service.spec.ts`, Gateway `JourneyScreenProxyHandlerTests.cs` | Wiring |
| Knowledge client mapping | `knowledge-base-data.service.spec.ts` | Client DTO mapping with fixture strings, not NABH text |

No Cypress (or equivalent browser) suite was found under the WebApp. Architecture docs describe manual browser QA as limited. **No test certifies that seeded sentences match an official NABH edition.**

---

## 10. AccredaOne relevance classification

### A. Accreditation knowledge

| Finding | Where | Implemented? | Authority | Informs AccredaOne? | Stay in Command? | Validation still required |
| --- | --- | --- | --- | --- | --- | --- |
| Pathway as framework + edition + platform id | `ComplianceFramework`, `Framework`, `MasterFrameworks.csv` | Model yes; only one KB tree generated | Names are internal labels. Edition string is not proven against the PDF. | Yes, as the top of the information architecture | The operational enrollment record stays in Command | Legal name use; which programmes are in scope; edition pinning |
| Chapter → standard → objective element | Both hierarchies | Yes, as a shape | Chapter codes are identifiable. Standard sentences unknown. OE text synthetic. | Yes. This is the strongest reusable structure. | Hospital-specific scores on those nodes stay in Command | Confirm edition and that codes match the chosen source |
| Core / Commitment / Achievement / Excellence | Enum, operational policy, wizard SQL | Rules yes; **content classification is not authoritative** | Product heuristic. Seed README defers to the PDF. | Yes, as a teaching dimension after validation | Mode-based workspace filtering can stay in Command | Map each element to the official tier or drop the labels |
| Requirement relationships | Parent ids on `ComplianceNode` | Hierarchy only | Structural | Yes for navigation | No | Confirm parentage was not damaged by the Entry Level id remap |
| Terminology | Code, chapter names, NC / OFI / CAPA language | Scattered | Mixed official terms and product terms | Use a controlled glossary. Do not assume Command labels are the learner’s language. | Operational status names stay in Command | Glossary review |

### B. Learning knowledge

| Finding | Where | Implemented? | Authority | Informs AccredaOne? | Exclusive to Command? |
| --- | --- | --- | --- | --- | --- |
| Explanation / why it matters / how to comply | `KnowledgeItem` and wizard step 0–1 | Fields and UI yes | Synthetic template; may embed standard titles | The **slots** inform the content model. The **sentences** should not be published as-is. | No, but current text is not a learning asset |
| Examples | — | NOT FOUND as a content type | — | Needed; Command does not supply them | — |
| Audit checklist as study prompt | `KnowledgeItem.AuditChecklist`, wizard ticks | Template plus checkbox UI | Synthetic | Shape of a self-check is useful. Text is not. | Checkbox completion tied to a hospital journey is Command |
| Learning sequences | OE wizard step order | One fixed five-step sequence per element | Product UX | Yes, as a prototype journey. It is execution-oriented (upload, submit). | Submit-to-journey is Command |
| Related concepts | Compliance graph | Document cross-references | Institutional | Not a learning graph. Do not reuse as “related topics”. | Graph of hospital documents stays in Command |
| Department navigation | CSV department labels; OE department chip | Labels yes | 10 buckets, 1:1 with roles, not surveyed | Optional navigation facet after review | Ownership matrix stays in Command |

### C. Readiness knowledge

| Finding | Where | Implemented? | Authority | Informs AccredaOne? | Exclusive? |
| --- | --- | --- | --- | --- | --- |
| Applicability and not-applicable | `IsApplicable`, NA reason on scores | Yes for hospitals | Operational | Teach the *idea* that not every element applies. Do not store a hospital’s NA decision. | The decision record is Command |
| Evidence expectations | Template + `EvidenceCategory` | Partial | Generic | Concept yes. Copy no. | File storage is Command |
| Self-assessment questions | `WizardQuestion` | Derived rows | Non-authoritative | Question *fields* (prompt, help, expected evidence, phase) are a useful schema | Scored hospital answers are Command |
| Readiness dimensions | Modes, maturity bands, percent, eligible flag | Several, inconsistent | Product rules | Teach dimensions (scope, evidence, practice). Do not present Command’s eligible flag. | Eligible / not eligible as an operational result is Command |
| Gap identification | `GapClassifier` | Yes | Heuristic | Severity language can inspire advisory feedback if relabelled and explained | Formal gap report for a facility is Command |
| Advisory outcome | Copilot rules, recommendation panels | Copilot exists for operators. Recommendation panel not mounted on current journey templates. | Rule engine, human approval in Command design | A transparent “this is advice” pattern is relevant. Command’s copilot is not that public product. | Proposals that create tasks are Command |

### D. Institutional execution — see section 11

---

## 11. Command-exclusive capabilities

These are implemented to run a hospital’s compliance system. Transferring them would collapse the product boundary in `AOVIAS.AccredaOne/README.md`.

| Capability | Evidence | Why it stays in Command |
| --- | --- | --- |
| Tenant onboarding and framework enrollment | `EnrollHospitalInFrameworkCommandHandler`, `/onboarding` | Creates organisational obligations |
| Journey lifecycle, approve / suspend / certificate fields | `ComplianceJourney`, `NabhJourneyPolicy` | Governance state |
| Obligation projection and task sync | `OrganizationComplianceObligation`, compliance tasks | Work management |
| Ownership, departments, SLA, assignment routing | `ObjectiveElementOwnership`, assignment services | Accountability |
| Evidence upload, hash, verification, binder | `Evidence`, `ObjectiveElementEvidence` | Institutional records. Binder checklist is empty, but the repository is real. |
| Controlled document lifecycle | `ControlledDocument` and related types | Legal document control |
| Audit sessions, simulation, findings | `AuditSession`, `Finding` | Assurance activity |
| Deficiency CAPA | `ComplianceDeficiency` | Corrective action system of record |
| Dashboards, snapshots, control center | `DashboardProjection`, reporting controllers | Management view of one organisation |
| Accreditation eligible flag | `NabhAccreditationEvaluator` | Can be read as a determination. Unsafe in a learning product without a strong disclaimer, and still the wrong job. |
| Copilot actions that propose operational changes | `ComplianceService/Application/Copilot/` | Operator assistant |
| Compliance graph of hospital documents | `EnterpriseComplianceGraphService` | Operational traceability |
| Workforce training matrix | `features/training` | HR compliance, not standards education |

---

## 12. Source provenance and rights findings

### 12.1 What is documented

`seed-data/README.md` states:

- Target narrative: NABH Hospital Accreditation Standard, 6th Edition (January 2025).
- An official PDF URL on the NABH portal.
- CSV working copies synced from `v:\DevelopmentRepo\New folder\` (outside this repo).
- Optional local PDF path `seed-data/source/nabh-hospital-6e.pdf` (not present in the tree that was searched).
- Knowledge text is produced from templates plus CSV joins.
- Official CORE / COMMITMENT tier should be confirmed in the PDF.

`manifest.json` names authority `QCI`, version `EntryLevel-2024`, published `2024-06-01`, checksum `sha256-placeholder`.

`framework-mapping.json` states that chapter, standard, and objective-element CSV rows reference the Entry Level framework id `2A244FB6-7095-4643-A7C0-A539419B3FEC`, and that the generator remaps them to Full Hospital 6th Edition id `0FE8A9C0-9EA0-4970-9097-D4F73D45DCA7`.

No licence, redistribution terms, attribution block, or `KnowledgeSource` entity was found. AccredaOne’s own README lists provenance as an open question.

### 12.2 How content was produced

| Artifact | Mechanism | Classification |
| --- | --- | --- |
| `MasterFrameworks.csv` | Extract dated 2026-04-06. 11 programme rows. | Unknown. Names only in this report. |
| `Masterchapters.csv` | 10 rows, all parented to the Entry Level framework id | Structural import. Chapter titles are identification-level labels. Rights unknown. |
| `Standards.csv` | 100 rows, non-empty sentence titles | **Unknown: official, paraphrase, or internal.** Highest rights risk. Not quoted here. |
| `ObjectiveElements.csv` | 503 rows, 2026-04-07 | **Synthetic.** Every title matches `Objective Element …`. Every description matches `Objective element … requirement.` |
| Department, role | 10 + 10 labels, identical counts | **Illustrative assignment**, not a RACI study |
| Evidence type, frequency | 503 × Document, 503 × As Applicable | **Not differentiated.** Template variety is unused. |
| `seed-nabh-hospital-6e.sql` | `Generate-NabhSeed.ps1` | Generated. Republishes the Entry-Level-keyed tree as 6th Edition. |
| `KnowledgeItem` rows (503) | Templates + interpolation of standard titles | **Synthetic shell around unknown standard titles** |
| `RequirementDetail` (100, standards only) | Intent = standard title; measurable elements empty | Not an interpretation layer |
| `FrameworkClassifications` (10) | Chapter names | Mis-applied table. Not maturity classes. |
| `seed-wizard-questions-nabh-6e.sql` | INSERT…SELECT from the above | Derived, non-authoritative |
| `classify-wizard-questions-nabh-6e.sql` | Proportional band split | **Internally authored heuristic** |
| `nabh-entry-2024.json` | Hand-maintained, 5 chapters / 5 elements | **Internally authored sample.** Not the full Entry Level programme. |
| Full 6E Compliance catalog | Clone of Entry Level platform id | **Not** the 503-element Knowledge Base |
| Evidence upload files named “NABH Modelling” under `ComplianceService/App_Data/evidence_uploads/` | Local upload artifacts | Not a content source. Do not mine them as accreditation text. |

### 12.3 Flags (unknown provenance or rights)

1. Right to store, transform, or show `Standards.csv` titles, including inside generated Knowledge Items and wizard questions: **undocumented**.
2. Whether those titles are verbatim from the January 2025 PDF: **not verified**. The PDF is not in the repo.
3. Whether the 10-chapter / 100-standard / 503-element tree is actually 6th Edition Full Hospital content, or another extract attached to the Entry Level id and then relabelled: **unresolved**. Scale resembles a full hospital catalog. The foreign key does not.
4. Entry Level JSON checksum is a placeholder. Sample codes (ACC, ICC) do not match the 6E chapter codes (AAC, IPC).
5. Maturity bands on wizard questions are not traced to the official element tier.
6. No field records source locator (page, clause, paragraph) for any node.
7. Prior assessment `ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` correctly noted missing citation fields and the 613-node shape. It should not be used for its recorded git SHA or as permission to publish. This report adds the synthetic-OE finding, the Entry Level remap, the classification-table misuse, and the Compliance-versus-Knowledge Base catalog split.

**Working rule for the Product Owner:** nothing in the Command database or seed SQL is automatically licensed for AccredaOne.

---

## 13. Gaps and opportunities

| Theme | What Command has | Gap | Address by reorganising concepts? | Needs new research or source validation? |
| --- | --- | --- | --- | --- |
| Knowledge discovery | Chapter tree inside a hospital journey; catalog admin screen | No public, pathway-first discovery. 10 of 11 CSV programmes have no tree. | Navigation pattern can be reused | Which pathways, and licensed structure for each |
| Natural-language search | `LIKE`, 10 rows, four fields, over template text | Weak recall. Cannot answer “what does admission assessment require?” against real language. | No | New index only helps after real text exists |
| Requirement explanation | Slots: explanation, why it matters, intent | Slots are filled with placeholders and copied titles. Measurable elements empty. | Slot model yes | Author or license real interpretations |
| Connected learning | Parent hierarchy only | No prerequisites, contrasts, “often confused with”, or cross-chapter links | Hierarchy is the seed of a map | Relatedness is new editorial work |
| Visual knowledge navigation | Execution panels and a document graph | Graph is not a knowledge map. No learner-facing canvas of the standard. | Chapter map can be redesigned from the tree | Visual language is a product design task, not a port |
| Evidence understanding | Generic document checklist; upload UI | No per-element examples of good evidence. Interview/observation unused. Binder checklist stub. | Evidence-type vocabulary yes | Real expectations need source-backed authoring |
| Guided learning | Five-step OE wizard | Steps assume a hospital will upload and submit | Reorder into a read/reflect path | Examples, practice, feedback copy are new |
| Readiness self-reflection | Wizard score and KB assessment | Both persist operational or facility scores and can look like a verdict | A non-persistent reflection can reuse question fields | Items must be written for learning, not cloned from synthetic rows |
| Advisory feedback | Eligible flag, gap Critical/Major, copilot | Language is determinative. Thresholds are internal. | Explain *why a practice looks thin* without a pass/fail accreditation | Wording and rubric need a product decision |
| User progression | Maturity unlock and journey state for a hospital | No learner progress, bookmarks, or “continue where you left off” independent of a tenant | No | New if AccredaOne has accounts; otherwise deferred |
| Accessibility and usability | Some ARIA in the wizard | Not verified. Execution density is high for a newcomer. | Journey can be simplified | Usability testing is new |
| Source traceability | PDF URL in a README only | No locator on the node | Add citation fields in the conceptual model | Must be populated from a permitted source |
| Multi-framework | CSV names for 11 NABH programmes; generic entities | Only one generated tree, and it is mis-labelled relative to its source id. JCI not seeded. | Entity shape is generic enough | Each additional framework is a content programme, not a config flag |
| Multi-industry | Hospital-only departments and roles | No industry abstraction | Department facet could become “context” | New |
| International applicability | NABH-specific evaluator and seed | No locale, no alternate standard family in data | Keep the hierarchy generic | Market choice is a Product Owner decision |

**Reorganise now (concepts only, no content copy):** hierarchy, evidence-type vocabulary, the distinction between expectation and repository, the distinction between self-reflection and formal audit, maturity as a lens rather than a score, pathway → structure → element.

**Do not reorganise into AccredaOne until validated:** standard titles, any string generated from them, wizard questions, maturity band assignments, department/role pairings, numeric accreditation rules.

---

## 14. Recommended AccredaOne learning and readiness journey

AccredaOne’s approved direction is a knowledge and readiness experience, not a second Command. The recommendation below uses Command as evidence of what is already understood, and refuses to treat Command content as the course.

| # | Stage | Recommendation | Why |
| --- | --- | --- | --- |
| 1 | Discover an accreditation pathway | **Essential for MVP**, thin | Command already thinks in named pathways (`MasterFrameworks.csv`) but only one broken-provenance tree exists. MVP can offer a single clearly labelled pathway once rights and edition are decided. A catalogue of 11 programmes is not supported by content. |
| 2 | Understand its structure | **Essential** | Chapter → standard → element is implemented and is the right mental model. Ship codes and structure only after the edition is confirmed. Do not draw the map from the Entry Level JSON sample and the 6E seed at the same time; they disagree. |
| 3 | Explore a requirement or topic | **Essential** | The execution screen proves people need to enter through a chapter and a code. AccredaOne should add topic entry (department or question), which Command does not do well. |
| 4 | Understand meaning and intent | **Essential**, content is new | The wizard already has this step, which shows the product instinct is right. The stored paragraphs are not usable as the meaning. |
| 5 | Explore applicability and context | **Optional in MVP** | Department and role fields exist but are a 10-by-10 illustrative grid, and frequency is uniformly “As Applicable”. Teaching “this often involves nursing” is useful later. Do not present it as the hospital’s scope decision. |
| 6 | Understand evidence expectations | **Essential as understanding, not as a checklist engine** | Separating “what good proof looks like” from “upload the file” is the main conceptual gift from Command, because Command itself blurs them. MVP should explain evidence types in plain language. The current Document paragraph is too generic to ship. |
| 7 | Access related learning resources | **Deferred** | Not found as a learning graph. Building it by copying the compliance graph would show hospital document links, which is the wrong object. |
| 8 | Guided preparation or self-reflection | **Optional early, essential before calling the product a readiness platform** | Command shows demand (wizard and KB assessment) and also the trap (scores that resemble a verdict). A short, non-persistent reflection on a handful of validated elements is enough to test the idea. Do not seed it from `WizardQuestion`. |
| 9 | Transparent advisory feedback | **Optional with stage 8** | Gap categories and the eligible flag are the wrong tone. Feedback should say what was considered and that it is not an accreditation decision. Command has not shipped a clean advisory voice on the current journey screen (recommendation panel is unmounted; smart assist is a placeholder). |
| 10 | Relevant next steps | **Deferred in institutional form; a small learning “what to read next” is optional** | Command next steps are owners, tasks, and CAPA. Those stay in Command. A “next element in this chapter” link falls out of stage 2 once the tree is real. |

**MVP, if the goal is to prove the knowledge experience:** stages 1–4 and a honest version of 6, on one pathway, with newly validated content, citation visible, and no score that says “accredited” or “eligible”.

**Add when the knowledge holds:** stage 8 and 9 on that same small set.

**Leave until a later release:** 5 as a rich facet, 7, learner accounts and progression, multi-programme catalogue, and anything that stores institutional evidence or assigns work.

---

## 15. Unresolved questions and decisions

1. Is AccredaOne allowed to display NABH chapter names, standard statements, or objective-element text, and under what licence? **Undocumented.**
2. Which single edition is the MVP source of truth? The seed says 6th Edition January 2025, the CSV parent id says Entry Level v1.0, and the Compliance JSON says EntryLevel-2024 with different codes.
3. Will official wording be shown, paraphrased, or replaced by original teaching text? Command currently mixes all three without labelling them.
4. Are Core / Commitment / Achievement / Excellence in scope as learner filters before they are checked against the PDF element by element?
5. Should any numeric rule (80 percent, score 4, one low score) ever appear? They are implemented as Command policy, not as cited NABH clauses.
6. Is the first audience a hospital quality officer learning the standard, a student, or a consultant? Command’s UI assumes the first, already inside a tenant.
7. Does “readiness” in AccredaOne mean personal understanding, or a preview of institutional readiness? The second overlaps Command and needs a sharp disclaimer.
8. Is one NABH hospital pathway the only MVP, or is multi-programme part of the promise? Source data supports names only.
9. Who may approve public text, and where does citation live, given Knowledge Base has no provenance fields and AccredaOne must not read Command databases directly (`AOVIAS.AccredaOne/README.md`)?
10. Should the empty measurable-elements field and the misused `FrameworkClassifications` table be treated as defects in Command, or ignored because AccredaOne will not share that database? They matter only as warnings against reuse.

---

## 16. Evidence-backed recommendations

1. **Keep Command’s hierarchy vocabulary.** Pathway, chapter, standard, objective element, evidence expectation, applicability, and self-check are already designed. AccredaOne does not need a new ontology to start.
2. **Do not migrate Knowledge Base rows into AccredaOne.** Objective-element text is synthetic. Standard titles are uncleared. Wizard questions and knowledge items inherit both.
3. **Do not clone the Full 6E Compliance catalog as if it were the standard.** The replication seeder copies Entry Level master data. The checked-in Entry Level file is a five-element sample.
4. **Treat the OE wizard step order as a UX lesson, not a content source.** Understanding and evidence-expectation steps match the AccredaOne vision. Upload, audit submit, and review-and-submit belong to Command.
5. **Keep scores, eligibility, CAPA, evidence files, tasks, and audit sessions exclusive to Command.** They answer “what did this hospital do?” AccredaOne answers “what does this requirement mean, and how would someone prepare?”
6. **If readiness advice is included, label the inputs and withhold a pass/fail accreditation result.** Command’s own evaluator is explicit and tested, which makes it easy to misuse as an official rule.
7. **Do not invest in search, related-content, or multi-framework scale until a small set of rights-cleared, cited elements exists.** Today’s search indexes template prose and returns at most ten rows.
8. **Next investigation is editorial and legal, not technical.** Compare a sample of standard codes to the permitted source document; decide paraphrase versus quotation; design citation fields; only then author explanations. No production stack should be chosen for that work.
9. **Ignore `ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` as an implementation go-ahead.** Its structural description of Knowledge Base is still broadly right. Its implied readiness to publish NABH pages is not, given provenance findings in this report.

---

## 17. Appendix — paths, symbols, endpoints, tests

### Product and provenance

| Item | Path |
| --- | --- |
| AccredaOne scaffold | `AOVIAS.AccredaOne/README.md` |
| Prior KB assessment | `ACCREDACONE_KNOWLEDGEBASE_READINESS_ASSESSMENT.md` |
| Seed README and PDF link | `AOVIAS.Command/ComplaincePlatform/seed-data/README.md` |
| Generator | `…/seed-data/tools/Generate-NabhSeed.ps1` |
| Templates | `…/seed-data/config/knowledge-item-templates.json` |
| Remap note | `…/seed-data/config/framework-mapping.json` |
| CSV inputs | `…/seed-data/source/MasterFrameworks.csv`, `Masterchapters.csv`, `Standards.csv`, `ObjectiveElements.csv` |
| KB seed | `…/seed-data/sql/seed-nabh-hospital-6e.sql` |
| Wizard derivation | `…/seed-data/sql/seed-wizard-questions-nabh-6e.sql` |
| Band classifier | `…/seed-data/sql/classify-wizard-questions-nabh-6e.sql` |
| Expected counts | `…/seed-data/sql/verify-rowcounts.sql` (1 framework, 10 chapters, 100 standards, 503 objective elements, 100 requirement details, 503 knowledge items) |
| Compliance clone script | `…/seed-data/sql/sync-compliance-catalog-nabh-full-6e.sql` |
| Entry manifest and sample | `…/Compliance.MasterData/Data/NABH/EntryLevel/manifest.json`, `nabh-entry-2024.json` |
| Entry seeder | `Compliance.MasterData.Seeders.NABH.NabhEntryLevelSeeder` |
| Full clone seeder | `NabhFull6eCatalogReplicationSeeder` (`CP00000002` → `CP00000001`) |

### Domain symbols

| Symbol | Path |
| --- | --- |
| `ComplianceFramework`, `ComplianceChapter`, `ComplianceStandard`, `ObjectiveElement`, `ObjectiveElementClassification` | `ComplianceService/Domain/` |
| `ComplianceJourney`, `NabhJourneyPolicy`, `ObjectiveElementScore`, `OrganizationComplianceObligation` | same |
| `Evidence`, `ObjectiveElementEvidence`, `Finding`, `ComplianceDeficiency`, `ActionPlan`, `ControlledDocument` | same |
| `OperationalModeClassificationPolicy` | `ComplianceService/Domain/Policies/OperationalModeClassificationPolicy.cs` |
| `NabhAccreditationEvaluator` | `ComplianceService/Application/Journey/NabhAccreditationEvaluator.cs` |
| `DeterministicNabhScoringService` | `ComplianceService/Application/Services/DeterministicNabhScoringService.cs` |
| `ComplianceScoreService`, `AggregationService` | `ComplianceService/Application/` |
| `Framework`, `ComplianceNode`, `RequirementDetail`, `KnowledgeItem`, `WizardQuestion`, `FrameworkClassification`, `ScoringConfig` | `KnowledgeBase/Domain/Entities/` |
| `GapClassifier` | `KnowledgeBase/Application/GapAnalysis/GapClassifier.cs` |
| `AssessmentSummaryCalculator` | `KnowledgeBase/Application/AssessmentSummary/AssessmentSummaryCalculator.cs` |
| `QuestionnaireGenerator` | `KnowledgeBase/Application/Questionnaire/QuestionnaireGenerator.cs` |
| `KnowledgeRepository.SearchAsync` | `KnowledgeBase/Infrastructure/Repositories/KnowledgeRepository.cs` |
| `EvidenceBinderQueryService.GetChecklistAsync` | returns `Array.Empty` |
| `EnterpriseComplianceGraphService` | `ComplianceService/Application/Services/EnterpriseComplianceGraphService.cs` |

### Framework identifiers (traceability)

| Id | Meaning in source |
| --- | --- |
| `0FE8A9C0-9EA0-4970-9097-D4F73D45DCA7` | Knowledge Base target: NABH Full Accreditation (Hospital), 6th Edition. Also CSV row `CP00000001`. |
| `2A244FB6-7095-4643-A7C0-A539419B3FEC` | CSV parent of all 10 chapters: NABH Entry Level Accreditation (Hospital), Entry Level v1.0, `CP00000002`. |
| `CP00000001` / `CP00000002` | Operational platform ids used by the clone seeder. Not proof that operational rows equal the 503-element tree. |

### Journey and knowledge endpoints (representative)

| Method and route | Symbol |
| --- | --- |
| `POST api/v{version}/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll` | `FrameworkController.EnrollHospitalInFramework` |
| `POST …/hospitals/{hospitalId}/frameworks/{frameworkId}/journeys` | `JourneysController.CreateJourney` |
| `GET/PATCH api/v{version}/journeys/{journeyId}/session` | journey session / operational mode |
| `GET …/journeys/{journeyId}/chapters` | structure |
| `GET/PUT/POST …/journeys/{journeyId}/oe-execution-wizard` | OE wizard |
| `GET/POST …/journeys/{journeyId}/objective-elements/{id}/evidence` | OE evidence |
| `GET api/v{version}/knowledge/node/*`, `…/knowledge/search` | `KnowledgeController` |
| `POST api/v{version}/assessments/{assessmentId}/gap-analysis` | KnowledgeBase assessments |
| `GET api/v{version}/reports/nabh-readiness` | `ReportingController` / `GetNabhReadinessReportQueryHandler` |
| `GET/PATCH api/v{version}/deficiencies/*` | CAPA via deficiency |
| `GET/POST api/v{version}/auditor/*` | `AuditorController` |

### Frontend symbols

| Symbol | Path |
| --- | --- |
| Journey routes | `WebApp/src/app/features/journey/routes.ts` |
| `OE_WIZARD_STEP_LABELS` | `…/journey/models/oe-execution-wizard.model.ts` |
| `OeExecutionWizardComponent` | `…/journey/components/oe-execution-wizard/` |
| `knowledge-base-oe-wizard.mapper.ts` | maps Knowledge Base DTO into the wizard, with `KB_SCORE_SCALE_PRODUCT_NOTE` |
| `KnowledgeBaseDataService` | `…/journey/services/knowledge-base-data.service.ts` |
| Onboarding | `WebApp/src/app/features/onboarding/` |
| Gap UI | `WebApp/src/app/features/gap-analysis/` |

### Tests cited

| File | Asserts |
| --- | --- |
| `ComplianceService.Tests/Journey/NabhAccreditationEvaluatorTests.cs` | Eligibility rules |
| `ComplianceService.Tests/Journey/OperationalModeClassificationPolicyTests.cs` | Band visibility |
| `ComplianceService.Tests/ComplianceScoreServiceParityTests.cs` | OE score SSOT |
| `ComplianceService.Tests/OeWizardContractTests.cs` | Wizard score contract |
| `ComplianceService.Tests/ComplianceDeficiencyCapaLifecycleTests.cs` | CAPA |
| `ComplianceService.Tests/CapaVerifyRoleGateTests.cs` | Who may verify |
| `ComplianceService.Tests/EvidenceVerificationStateMachineTests.cs` | Evidence review states |
| `ComplianceService.Tests/FindingPromotionServiceTests.cs` | Finding promotion |
| `ComplianceService.Tests/Authorization/EnrollHospitalAccessTests.cs` | Enrollment tenancy |
| `Knowledgebase.Tests/GapClassifierTests.cs` | Gap bands |
| `Knowledgebase.Tests/AssessmentSummaryCalculatorTests.cs` | Assessment rollup |
| `Knowledgebase.Tests/KnowledgeApiIntegrationTests.cs` | Gap-analysis POST |
| `WebApp/src/app/features/journey/services/knowledge-base-data.service.spec.ts` | Client knowledge HTTP |
| `WebApp/src/app/features/journey/services/journey-data.service.spec.ts` | Journey URL contracts |

### Counts established in this investigation

| Fact | Evidence |
| --- | --- |
| 503 objective elements, all synthetic titles and descriptions | Read-only aggregation of `ObjectiveElements.csv` |
| 100 standards, 0 empty titles | Same, titles not printed |
| 10 chapters, all `FrameworkId` `2A244FB6-7095-4643-A7C0-A539419B3FEC` | `Masterchapters.csv` |
| 10 departments and 10 roles with matching counts (110, 78, 60, 55, 45, 44, 36, 30, 25, 20) | OE CSV. Largest pair: Clinical / Nursing with Chief Medical Officer |
| Evidence type Document and frequency As Applicable on all 503 | OE CSV |
| Entry JSON objective element codes | `PRE.1`, `ACC.1`, `ICC.1`, `HRM.1`, `FMS.1` only |

---

## Closing answers

**What Command has solved conceptually.** A hospital compliance system needs a versioned pathway, a chapter/standard/element tree, a distinction between reading a requirement and producing evidence, a wider self-assessment view than a final-audit view, explicit gaps, named owners, and a path from finding to corrective action.

**What it has actually implemented.** That operating system, with tested pieces around eligibility math, operational-mode filters, CAPA, evidence verification, and assessment gap classification. The objective-element screen already tries to teach (explanation, why it matters, how to comply, evidence expectations) and then turns into execution.

**What AccredaOne can learn.** The tree, the evidence-versus-repository distinction, the self-check versus formal audit distinction, and the wizard’s early steps as a learning sketch.

**What should not be transferred.** Enrollment, obligations, files, tasks, audits, findings, CAPA, dashboards, eligibility determinations, and the current knowledge sentences.

**What is still missing.** Rights, edition proof, real objective-element language, real interpretations, per-element evidence examples, related-topic links, a non-determinative advisory voice, and any second framework with content.

**What to investigate next.** A rights decision, then a line-level comparison of a small code sample to the permitted source, then an editorial model for citation. Not a service design.

**Decisions the Product Owner needs to make.** The ten questions in section 15. The ones that block everything else are licence, edition, and whether AccredaOne is allowed to show a score.

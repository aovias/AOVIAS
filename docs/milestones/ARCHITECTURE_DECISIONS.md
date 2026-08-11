# AOVIAS — Architecture Decision Records
### Binding decisions. Violating an ADR requires superseding it here first — never silently in code.

Format per record: **Status · Context · Decision · Consequences · Evidence.** Statuses: Accepted, Proposed, Superseded-by-NNNN. New ADRs are appended with the next number; existing ADRs are never edited except to change Status.

---

## ADR-0001 — Layered architecture with no bypasses
**Status:** Accepted.
**Context:** The platform spans publishing through fleet operations; uncontrolled cross-dependencies were the root of early duplication (compose values from code, agent folders from settings).
**Decision:** Nine layers — Publishing → Contracts → Planning → Provisioning → Execution → Operations → Fleet → Cloud → AI. Each layer consumes only the previous layer's published interfaces. A layer may be incomplete; it may never be circumvented.
**Consequences:** New capabilities are placed in a layer before they are designed; UI and scripts are consumers of APIs, never actors on lower layers; grep gates enforce known bypass patterns.
**Evidence:** `ROADMAP.md` §3; historical violations catalogued in `MILESTONE-2-…md` §2.2.

## ADR-0002 — Publishing is the only contract writer; stages that read never write
**Status:** Accepted.
**Context:** Infrastructure truth was written from four places (registry code, settings, bash, compose builder).
**Decision:** Only the Publishing pipeline compiles and seals contracts into packages. Every downstream stage (DRV, planner, agent, certification, reconciliation) is read-only over contracts. Lifecycle stages 4–12 may only read.
**Consequences:** Any contract mutation outside Publishing is a defect; environment-specific values arrive at deploy time via bindings/.env, never by editing contracts.
**Evidence:** M2 spec §4/§6; M2.0 report (contract authority).

## ADR-0003 — The sealed `infrastructure.contract.json` is the single consumed artifact
**Status:** Accepted.
**Context:** The aggregate contract was originally written but never read; agent and DRV rebuilt contracts in memory, creating dual construction paths (`ReleaseDeploymentProvider.cs:434`, pre-M2.0).
**Decision:** Consumers read the sealed, checksummed, fingerprint-covered contract from the package through one resolver. Deploy-time rebuilding is banned.
**Consequences:** Publish-time and run-time views cannot diverge; tampering fails checksum verification; the 13 per-concern files are compatibility projections slated for retirement.
**Evidence:** M2 spec §7 (Critical gap #1); M2.0 implementation report.

## ADR-0004 — Requirements vs. Capabilities (open-world model)
**Status:** Accepted.
**Context:** `FeatureContract`/`RuntimeCapabilities` were closed boolean sets; every new infrastructure concept was a breaking model change, and nothing described what an *environment* provides.
**Decision:** Services declare typed **requirements** (`kind` + payload + provider binding); environments declare **capabilities**; the planner/admission engine matches them. New infrastructure concepts are new kinds in a governed registry, not new model fields.
**Consequences:** Kubernetes/cloud/air-gapped targets are capability adapters, not redesigns; infrastructure services (Redis, RabbitMQ) can `provide` capabilities; admission can block deterministically before any mutation.
**Evidence:** M2 spec §3.2/§9; M2.3 implementation report.

## ADR-0005 — Infrastructure is declared as data, never as platform code
**Status:** Accepted.
**Context:** All ten services' infrastructure lived in a hardcoded C# switch (`ServiceContractRegistry`, 507 lines) plus service-id special cases (`ai` providers, identity builder); adding a service meant re-shipping the contracts assembly.
**Decision:** Per-service specification files (infrastructure specification language, M2.1) + shipped platform profiles are the only declaration mechanism. Platform assemblies contain zero service-specific knowledge (grep-gated).
**Consequences:** Customer services self-describe; PRAMAN is just data; transcription errors are caught by contract-equality gates during migration.
**Evidence:** M2 spec §7 (Critical gap #2), §12 M2.2 sprint; M2.1 report.

## ADR-0006 — Contract schema evolution: one Current writer, migrating readers
**Status:** Accepted.
**Context:** Long-lived packages and fleets guarantee version skew.
**Decision:** Exactly one Current schema version is written; readers accept Current + Supported versions via registered migrators; Deprecated → Removed requires republish, not migration. The same pattern governs package layout, plan format, and agent APIs (N−1 support).
**Consequences:** Upgrades are drills, not events; skew is checked at admission; removal is deliberate and documented.
**Evidence:** `CONTRACT-SCHEMA-EVOLUTION-POLICY.md`; M2.2 report; ROADMAP §10.

## ADR-0007 — Planning decides; provisioning and execution only obey
**Status:** Accepted.
**Context:** Early provisioning code paths made ad-hoc decisions (create-if-missing behaviors scattered), making outcomes environment-dependent and unauditable.
**Decision:** The Deployment Planner + Admission Engine produce an immutable, versioned, hash-addressed plan. Provisioners and the execution runtime execute approved plans exactly; they never re-plan, re-order, or improvise. Unknown plan versions are rejected, not best-effort executed.
**Consequences:** Deployments are replayable and auditable; failures point at either a wrong plan or a failed step — never a hidden decision; rollback can reuse the same machinery (ADR-0013).
**Evidence:** M2.4 report (admission separates decision from execution); M2.5 report (agent as deterministic executor).

## ADR-0008 — Provider abstraction for every host action
**Status:** Accepted.
**Context:** Direct Docker/filesystem calls from core logic would weld the platform to one OS and one orchestrator.
**Decision:** All host/orchestrator interaction flows through provider interfaces: `IInfrastructureExecutionContext` (engine, network, images, DB/bus reachability), secret/certificate providers, repair executors, and target bindings. Offline validation passes `Execution = null`.
**Consequences:** DRV and the agent share one pipeline with different contexts; new OS/orchestrator support means new providers, not new pipelines; core logic is testable without hosts.
**Evidence:** `IInfrastructureExecutionContext.cs`; M2 spec §10 tiers T2/T3; M2.5/M2.6 executor patterns.

## ADR-0009 — One validation rule engine, five tiers, zero duplicated rules
**Status:** Accepted.
**Context:** Validation risk was duplication across publishing, DRV, agent, and certification.
**Decision:** T0 schema → T1 semantic (shared `ContractValidator` core) → T2 environment-independent simulation (DRV) → T3 environment-bound (agent + admission) → T4 post-deploy verification (certification/drift). Each tier adds only information the previous tier could not have. Every rule exists in exactly one place; all tiers report through one blocker/result shape.
**Consequences:** CSM renders one readiness language; a rule change propagates everywhere automatically; projection checks (e.g., compose mount validation) are classified as binding validators, not contract rules.
**Evidence:** M2 spec §10; existing shared pipeline reuse between DRV and agent.

## ADR-0010 — Compose (and every orchestrator format) is a projection, not a source of truth
**Status:** Accepted.
**Context:** docker-compose literals had leaked into the portable model (`${AOVIAS_DOCKER_NETWORK}` token, host-token mounts) and compose carried baked publisher-profile paths (M1 N7).
**Decision:** Layer-2 contracts are orchestrator-neutral. Target bindings (`IDeploymentTargetBinding`) project contracts + plans + environment values into compose today, Kubernetes/ACA/ECS/Nomad later. Bindings are produced per environment at deploy time and are never authored or hand-edited.
**Consequences:** New targets are new bindings; no Docker literal may exist in the portable model; compose ceases to be a hand-inspectable contract — the sealed contract is.
**Evidence:** M2 spec §5 Layer 3 / §8; M1 spec N7/C2.

## ADR-0011 — The Release Library is the authoritative artifact repository with one registration path
**Status:** Accepted.
**Context:** Two uncoordinated copy implementations (organizer + CSM flat copy) and no lifecycle/index existed.
**Decision:** A single `IReleaseLibraryService` backed by a persisted index owns registration (idempotent, collision-detecting), lifecycle (Building→…→Deployed, with Deprecated/Archived branches), promotion status, queries, integrity, and conservative two-phase retention. All entry points (pipeline, solution orchestrator, CSM/SDK) delegate to it.
**Consequences:** "What is deployable/latest/rollback-eligible" is answered by the index, never by directory scans; uncertified packages cannot enter; retention can never delete deployed/certified/rollback-candidate artifacts.
**Evidence:** M1.5 implementation report; M1 spec D9.

## ADR-0012 — Determinism with a declared volatile set; identity over bytes for images
**Status:** Accepted.
**Context:** Binary image tars are not byte-stable across builds; hashing them made the golden gate cry wolf and trained rubber-stamping.
**Decision:** The deterministic fingerprint covers normalized text artifacts + contract set + **image identity (digest/build manifest)**, with an explicitly versioned volatile-entry policy (metadata, certificate, publishing report). Every image carries a source-derived build manifest verified post-build. Golden mismatches produce a recorded approve/reject decision, never a silent overwrite or unrecoverable failure.
**Consequences:** Unchanged source ⇒ identical fingerprint for all services; re-baselining is a controlled, audited event; determinism regressions are CI failures.
**Evidence:** M1 spec §3 (N1–N3) and §8.5 steps 2–3, 8; M1.2 report.

## ADR-0013 — Rollback is a plan, executed by the same machinery as deployment
**Status:** Accepted (implementation target: M6).
**Context:** A pre-plan-era rollback path exists in `Aovias.Release`; parallel rollback machinery would violate ADR-0007 and double the failure surface.
**Decision:** Rollback is planned, admitted, provisioned, executed, health-verified, and certified through the identical pipeline as forward deployment, gated by the contract's recovery domain (automatic only for `stateless`/`data-compatible`; `manual` requires operator confirmation). The legacy path delegates or is deleted.
**Consequences:** One execution truth; rollback inherits every safety property of deployment; recovery classes become a service-authored contract concern.
**Evidence:** ROADMAP M6; M2 spec §6 stage 11; M1.5 report (Aovias.Release confirmed untouched/out-of-scope).

## ADR-0014 — Reconciliation: desired state only from approved plans; repairs gated by policy then safety
**Status:** Accepted.
**Context:** Two earlier watchers (Docker-overview poller; deployment-status drift check) alerted without authority or repair, and risked a third competing loop.
**Decision:** Desired State derives exclusively from the approved DeploymentPlan + sealed contract + deployment metadata. Observed State discovery is read-only and provider-backed. Drift classification has one source of truth (`DriftRepairMatrix`). Repairs are planned (not executed) by the reconciliation planner, pass an environment-aware policy evaluation, then a final safety gate that may downgrade to operator approval, and only then dispatch to `IRepairExecutor` providers. Cycles never overlap; every cycle emits structured events and a replayable history record.
**Consequences:** Self-healing can never act on unapproved intent; prod defaults conservative; a second reconciliation implementation is banned (the remaining `DetectDriftAsync` overlap is scheduled for consolidation in M3.3).
**Evidence:** M2.6 implementation report §1/§9.

## ADR-0015 — Platform-first: zero workload knowledge in platform assemblies
**Status:** Accepted.
**Context:** PRAMAN is the first workload; healthcare specifics in platform code would forfeit the platform's purpose (and did exist as service-id switches).
**Decision:** No PRAMAN/healthcare identifiers, service lists, or domain branches anywhere in platform assemblies. Workload knowledge enters only via specification files and capability declarations. Enforced by grep gates in CI.
**Consequences:** Any enterprise application deploys the same way; demo-bundle membership and dependencies are data; PRAMAN can be removed from the repo without touching platform code.
**Evidence:** M1 spec C1/C5 (violations found); M2.2/M2.3 removals; ROADMAP principle 3.

## ADR-0016 — Typed failures with recovery actions; operators never read raw tool output
**Status:** Accepted.
**Context:** The vision forbids operators debugging via Docker logs; early failures surfaced stderr directly.
**Decision:** Every failure at a layer boundary is a typed result (category, message, recommendation, recovery action) produced by classifiers owned per layer. Raw process output is retained in structured logs for engineers, never as the operator-facing message.
**Consequences:** Deterministic recovery guidance is a schema property, testable by fault-injection matrices; UX consistency across publish/deploy/reconcile failures.
**Evidence:** `PublishingFailureClassifier`, `DeploymentOperatorError`, `InfrastructureStageResult.RecoveryActions`; M5.3 sprint (completion audit).

## ADR-0017 — Verification is evidence-based; traced-only code never merges (post-M3)
**Status:** Accepted.
**Context:** Sandbox constraints forced several sprints to ship "traced correct, not build-verified" (disclosed in M1.4/M1.5/M2.x reports), accumulating verification debt.
**Decision:** After the M3 baseline: build + full test output is mandatory acceptance evidence for every sprint; signature tracing is a review aid only; architecture claims require file/line evidence (the M1/M2 review standard).
**Consequences:** CI is a hard gate; sprint reports contain command output, not assertions; the M3 milestone exists to retire the existing traced-only backlog.
**Evidence:** M1.4/M1.5 sandbox disclosures; ROADMAP M3 + §9; guidelines §2/§6/§9.

## ADR-0018 — The platform deploys and updates itself
**Status:** Accepted (implementation target: M8.4).
**Context:** The Runtime Agent is itself a service; a platform that cannot upgrade its own agent safely fails its own promise.
**Decision:** The agent's infrastructure is declared in its own specification (it eats the same food); agent upgrades are fleet-managed, staged, health-gated, and self-rolling-back; bootstrap remains the recovery of last resort.
**Consequences:** No hand-maintained agent installs after M4; agent versioning/capability advertising (build manifest) is load-bearing; the riskiest mutation in the platform gets the strongest safety net.
**Evidence:** runtime-agent build manifest verification (M1); M2 spec T9; ROADMAP M8.4 risk entry.

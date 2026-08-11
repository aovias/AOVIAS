# AOVIAS — Implementation Guidelines
### The permanent handbook. Include this document (or its path) in every implementation prompt.

Governing companions: `ROADMAP.md` (what to build, in what order) and `ARCHITECTURE_DECISIONS.md` (why — binding decisions; violating an ADR requires superseding it first, never silently).

---

## 1. Engineering Rules (non-negotiable)

1. **Single Responsibility.** One component, one reason to change. If a class name needs "And", split it.
2. **Composition over inheritance.** Extend behavior via providers, strategies, and pipeline stages — the platform's established patterns.
3. **No parallel implementations.** Before writing anything, search for the existing implementation of the concept (the sprint reports' "Required Engineering Analysis" step is mandatory). If one exists: extend, delegate, or replace-and-delete. Two implementations of one concept is a defect even if both work.
4. **No duplicated models.** One model per concept, owned by one assembly. Downstream layers consume it; they do not mirror it. Projections/DTOs at API boundaries are allowed only when they serve a wire/UI need and are mapped in one place.
5. **No TODO implementations, no placeholder code.** Every merged member is functional. If scope forces omission, the member must not exist (and the omission goes in the sprint report), rather than existing as a stub.
6. **Backward compatibility.** Package layout changes are additive within a major version. Contract schema changes follow `CONTRACT-SCHEMA-EVOLUTION-POLICY.md` (one Current writer; readers accept Current + Supported via migrators). Public APIs support N−1.
7. **Deterministic behaviour.** Same inputs ⇒ same outputs for publishing, planning, execution, rollback, reconciliation. Timestamps, GUIDs, machine names go only into declared-volatile artifacts. `DateTimeOffset.UtcNow`/`Guid.NewGuid()` in a deterministic path is a review-blocking finding.
8. **Idempotent execution.** Every provisioning step, pipeline stage, repair, and bootstrap action can run twice with the same end state. Retry policies assume this; document any exception explicitly.
9. **Provider abstraction.** No direct OS/Docker/filesystem/network calls from core logic. Host actions go through `IInfrastructureExecutionContext`, repair executors, secret/certificate providers, or target bindings. New external touchpoints get a provider interface first.
10. **Layer separation.** Publishing → Contracts → Planning → Provisioning → Execution → Operations → Fleet → Cloud → AI. Consume only the adjacent layer's published interfaces. Specific bans: nothing but Publishing writes contracts or packages; provisioners/executors never make planning decisions; UI contains no business logic; reads never write (lifecycle stages 4+ are read-only over contracts).
11. **Platform first.** No PRAMAN/healthcare identifiers, service-id switches, or workload-specific branches in platform assemblies. Workload knowledge lives in per-service specification files only. (Grep-gated.)

## 2. Development Workflow

Every sprint follows: **Analyze → Architect → Implement → Verify → Regress → Report.**

1. **Analyze (required, before any code):** read the governing milestone spec section, relevant ADRs, and the *current* code. Find existing implementations of every concept touched. State findings in the report.
2. **Architect:** confirm the change fits the layer model and ADRs. A conflict means stop and surface it — do not "make it work".
3. **Implement:** smallest coherent change satisfying the sprint scope. Delete what you replace, in the same sprint.
4. **Verify:** `dotnet build` all affected solutions; run all affected test suites. "Traced correct" is never acceptance (post-M3).
5. **Regress:** add permanent tests for every new behavior and every fixed bug; run the standing regression suites (fingerprint, contract-consistency, grep gates).
6. **Report:** write the sprint implementation report (see §8) in `docs/milestones/`.

## 3. Code Quality Rules

- **Naming:** follow existing conventions exactly (`I{Concept}{Role}`, `{Concept}Contract`, `{Stage}Provisioner`, `{Concept}Result` with static `Success`/`Failure`/`Ready` factories). New names describe the concept, not the sprint.
- **Testing:** every public behavior has a test; test names state the invariant (`ComposeGenerator_is_deterministic_and_drops_version_field` is the house style).
- **Dependency Injection:** constructor injection only; register in the owning assembly's `DependencyInjection.cs`; no service location, no statics for stateful services (static pure functions are fine and established).
- **Logging:** structured logging with stable message templates; log decisions and state transitions, not loops; operator-facing failures use the typed error/recovery model — never raw tool output.
- **Error handling:** failures return typed results (`*Result`, `*Report` with errors + recovery actions) at layer boundaries; exceptions are for programming errors and are classified at stage boundaries (existing `FailureClassifier` pattern). Never swallow; never `catch {}` outside documented best-effort cleanup.
- **Configuration:** typed options bound at startup; snapshot per operation (no mid-pipeline `CurrentValue` re-reads in deterministic paths); no hardcoded paths — everything through profiles/contracts/manifests.
- **Versioning:** stamp engine/schema/plan versions where established; never mutate a versioned artifact in place.
- **Documentation:** XML doc comments on public contracts; behavioral invariants stated where they're enforced; update the governing spec/ADR when reality diverges — the docs are part of the change, not an afterthought.

## 4. Refactoring Rules

- **Remove code** when it has zero production references (prove with repo-wide search, excluding `bin`/`obj` and stale `aovias-config` snapshots) or when its replacement ships in the same sprint. Deletion is preferred over deprecation inside the platform.
- **Migrate** (side-by-side with a shim) only when external consumers exist (package layout, agent API, schema) — then follow the versioning strategy and register the removal target.
- **Keep compatibility** only for versioned public surfaces (packages, schemas, agent APIs), never for internal code — internals are refactored freely with regression evidence.
- Behavior-preserving refactors ship with proof of preservation (golden fingerprints, snapshot equality, existing suites green).

## 5. Testing Rules

| Level | Bar |
|---|---|
| **Unit** | Every new class with logic; pure functions preferred; no I/O. |
| **Integration** | Every cross-layer seam touched (publish→plan, plan→provision, etc.); real components, faked providers. |
| **Regression** | Permanent test per fixed bug and per determinism/consistency property; suites only grow. |
| **Scenario** | Induced-failure matrices for every failure classification path (the DRV/scenario-matrix pattern). |
| **End-to-End** | Golden path + failure/recovery path on disposable environments; 3× repeatability is the bar. |
| **Performance** | Respect budgets set in M9; no perf work without a measured baseline first. |

Tests must be deterministic (no sleeps-as-synchronization, no wall-clock or network dependence; fake the execution context).

## 6. Build Rules

- Build before completion: `dotnet build` on every affected solution, zero errors **and zero new warnings**.
- No broken projects: if a project can't build in the environment, the sprint cannot be reported complete — say so explicitly (M1.4/M1.5 disclosure style) and the sprint remains open.
- No ignored failures: a red test is fixed or the change is reverted; skipping/quarantining a test requires a debt-register entry with owner and target.

## 7. Technical Debt Rules

- **Allowed:** consciously deferred breadth (extra providers, OS matrices, UX polish) — registered in the sprint report's debt table with owner, trigger, and target milestone.
- **Forbidden:** duplicated implementations/models, layer bypasses, nondeterminism, placeholder code, unverified merges, missing audit on mutations, undated/unowned debt. These are fixed in the sprint that finds them.
- Debt is retired by deletion or replacement, never by accretion.

## 8. Acceptance Rules — standing Definition of Done for every sprint

A sprint is Done only when **all** of the following hold:

1. Sprint-specific acceptance criteria (from `ROADMAP.md` §6) met.
2. All affected solutions build; all suites green; grep gates pass.
3. New behavior covered by permanent tests; induced-failure paths exercised where applicable.
4. Everything replaced was deleted; repo-wide search proves no parallel implementation remains.
5. No ADR violated; any ADR change merged alongside the code.
6. Implementation report written in `docs/milestones/{Sprint}-IMPLEMENTATION-REPORT.md` including: executive summary, files-modified table with reasons, evidence for claims, verification results (actual build/test output), debt table, explicit out-of-scope confirmation.
7. Backward compatibility preserved per §1.6, or the versioned migration shipped with it.

## 9. Pull Request Rules

- **Quality:** one sprint (or coherent slice) per PR; no drive-by changes; diff readable top-to-bottom against the sprint scope.
- **Review:** reviewer checks against this document and the ADR log — architecture conformance first, style second. Any §1 rule violation blocks.
- **Evidence:** PR description links the sprint report and includes build/test output, new/updated test list, and the deletion proof for anything replaced. "Works on my machine" is not evidence; command output is.

## 10. Engineering Decision Rules

- **Evaluate against the horizon:** every design choice is tested against the V2 targets (Kubernetes, cloud, air-gapped, AI) — will this decision survive them? If not, choose the abstraction that will (requirement/capability, provider, binding), even when the concrete shortcut is cheaper today.
- **Trade-off philosophy:** long-term maintainability > short-term convenience; determinism > flexibility; explicitness > magic; deletion > deprecation; one good abstraction > three easy special cases. Simplicity matters — but the simplicity that counts is the operator's and the maintainer's, not the author's.
- **When in doubt:** check the ADR log; if the situation is genuinely new, make the smallest reversible choice, document it as a proposed ADR, and surface it for review rather than embedding it silently.
- **Escalation:** if a sprint cannot be completed without violating these guidelines, stop and report — a smaller compliant sprint beats a complete non-compliant one.

## 11. Execution Honesty Rule (non-negotiable)

An implementation model must never claim a build, test, or deployment succeeded unless it actually executed the corresponding tool and observed the result. "I traced the logic and it should pass" is not "it passed." When the environment cannot execute the real tool (no SDK, no network, no target host), the report must say so explicitly rather than presenting inferred confidence as evidence.

Every verification claim in a sprint report must be labeled with the level of evidence that actually backs it:

1. **Source Analysis** — read the code and reasoned about correctness (types line up, references resolve, logic traced by eye). No tool executed. Lowest evidence tier; sufficient only for findings, never for "passed"/"green" claims.
2. **Static Verification** — a deterministic, non-compiling check was actually run (grep gates, path/reference resolution scripts, schema/package-version diffing, dependency-graph enumeration). The tool ran and produced output; the output is the evidence, and it must be shown or summarized, not paraphrased from memory.
3. **Runtime Verification (requires SDK/environment)** — `dotnet build`, `dotnet test`, `dotnet publish`, container startup, or any check that requires the real .NET SDK, a runtime, or a live environment the sandbox does not have. If this tier cannot be executed, the report must tag the item **Requires SDK Execution** (or **Blocked**, with reason) — never **Verified** or **Passed**.
4. **End-to-End Verification** — the full deployed path exercised against a real or disposable target environment (golden-path + failure/recovery, per §5's repeatability bar). Highest tier; only claimed when actually run against a live target.

Rules that follow from this:
- A report may only use the words "Verified", "Passed", or "Green" for an item backed by tier 2, 3, or 4 evidence that was *actually executed in this session*. Tier-1 (source analysis) findings are reported as "reviewed" or "traced", never as "passed".
- If the sandbox cannot execute `dotnet` (or any other required tool), do not fabricate its output. State plainly which tier the environment supports, and mark every tool-dependent item **Requires SDK Execution** or **Blocked (reason)**.
- This tagging discipline applies to every sprint report going forward (§8.6) — the verification-results section must show the tier for each claim, not just a pass/fail summary.

## 12. Integration Sprint Progression (non-negotiable, effective M3.1 forward)

Whenever the roadmap enters an integration/verification phase — pausing new capability work to prove the platform actually works, as `ROADMAP.md` §5 M3 already does and as every later "prove it before extending it" phase must also do — the work follows the same four stages, in order, before any new capability sprint resumes:

1. **Build** — resolve all compiler errors; establish a trusted, green build baseline for every canonical solution.
2. **Test** — restore and execute the automated test suites against that build baseline.
3. **Runtime** — validate the built, tested artifact on a real target environment (a real VPS, not a simulation or a traced description of one).
4. **Certification** — repeat the successful deployment cycle enough times to prove it (per §5's repeatability bar), not just demonstrate it once.

This is a permanent addition to how the roadmap is executed, not a one-off sequence invented for M3. Rules that follow:

- A sprint report may not claim progress past the stage it actually reached. Reaching "Build" does not license "Test" or "Runtime" language; each stage's claims are still governed by the §11 evidence tiers (a stage isn't "done" on Source Analysis or Static Verification alone if its own definition requires Runtime or End-to-End evidence).
- New-capability sprints (the M4.x/M5.x/etc. feature work already scoped in `ROADMAP.md`) do not resume, and are not reported as advancing the roadmap, while an open integration phase has an unresolved earlier stage — Build blockers are not deferred past Test, Test failures are not deferred past Runtime, and so on.
- This progression re-triggers every time the roadmap accumulates a run of capability sprints on top of an unverified baseline — it is not consumed once by M3 and then forgotten for M6/M8/M9-adjacent verification passes. Each occurrence gets its own Build→Test→Runtime→Certification cycle scoped to what changed since the last one.
- The four stages may span multiple sprints (as M3.1/M3.2/M3.4′ already do) but must not be reordered or run out of sequence — Runtime validation is never attempted against a build that hasn't cleared Test, and Certification is never claimed off a single Runtime pass.

# Runtime Validation Report — Stage 5C.2

**Method, stated up front.** The Audit workspace sits behind the auth guard and there is no backend
running in this environment, so I could not drive the validation through a browser session. Instead
every case is executed against the **production services** — `PlatformConfigurationService` and
`AuditWorkflowEngine`, unmodified — with only the HTTP boundary replaced by a fake transport that
serves the same JSON the Configuration API returns and the seed scripts write.

That is weaker than a browser walkthrough in one specific respect and stronger in another. It does
not prove pixels render. It does prove that configuration flows through the real store, the real
version guard, the real signal graph and the real engine into the real accessors that components
bind to — repeatedly, on every commit, which a manual walkthrough does not.

**What this report does not certify:** rendered output, and anything downstream of an HTTP response
this environment cannot produce. §6 says what a signed-off session still needs to cover.

---

## 1. Headline

| | |
| --- | --- |
| Validation cases executed | **115** new, all passing |
| Total suite | **343 passing**, 1 pre-existing unrelated failure |
| Defects found | **4** — 1 Critical, 3 Medium |
| Defects fixed | **3** (1 Critical, 2 Medium) |
| Defects open | **1** Medium (DEF-CFG-004), with recommended fix |
| Build | AOT production build succeeds |
| Lint | 5 errors, all pre-existing `no-output-native`; 2 pre-existing unused imports removed |

---

## 2. DEF-CFG-001 — tenant configuration leakage · Critical · fixed

The one finding that would have blocked release.

**Root cause.** `PlatformConfigurationService.reset()` was written, documented and correct. Nothing
called it. `AuthService.clearLocalSession()` cleared session, user context, organization, compliance
context and lookup caches — but not configuration. `applyHospitalSwitch$` cleared nothing.

**Why no existing test caught it.** The service passes its own unit tests; `reset()` works when
invoked. The defect is entirely in the *absence of a call site*, which a unit test on the service
cannot express. This is worth noting as a category: every "clear on logout" collaborator is exposed
to the same blind spot.

**Evidence.**
- `auth.service.ts:167` — `clearLocalSession()` before the fix, no configuration reset.
- `app-context.service.ts:96` — hospital switch before the fix, no configuration reset.
- Grep for `.reset()` across `src/app` returned exactly one hit: a spec.

**Runtime reproduction.**
1. Sign in to Organization A, which has renamed `InternalAudit` to "Acme Review".
2. Open Audit → Planning. The type dropdown reads "Acme Review".
3. Sign out. Sign in to Organization B, which has its own configuration.
4. Open Audit → Planning. It still reads "Acme Review".

Step 4 is not a stale-cache flicker that resolves. The load-once guard (`loaded = true`) survived the
logout, so Organization B **never requests its own configuration at all** — the leak is permanent for
the session. The same sequence applies to a hospital switch without any sign-out.

**Risk.** Cross-tenant disclosure of configured business vocabulary — audit type names, status
labels, department lists, and since Wave 2 the entire workflow definition including stage names and
role restrictions. On a multi-tenant compliance platform this is a confidentiality failure, and the
workflow leak additionally means Organization B is operating under Organization A's lifecycle rules.

**Fix.** `platformConfiguration.reset()` in `clearLocalSession()`; `reset()` followed by `load()` in
`applyHospitalSwitch$`, positioned after the token reissue so the reload resolves under the new
tenant's token.

**Verification.** `configuration-session-boundary.spec.ts` drives the real `AuthService.logoutLocal()`
and asserts both that reset is called and that the store returns to platform defaults. **Confirmed as
a genuine regression guard**: with the fix reverted, both tests fail; restored, both pass.

---

## 3. Phase 1 — configuration validation matrix

Executed for **all 26 value categories**, each as three cases (78 total), plus 4 mechanism cases.

| Operation | Method | Result |
| --- | --- | --- |
| **Read** platform default | every category, label resolution | 26/26 pass |
| **Override** by tenant | publish a tenant row, load, read | 26/26 pass |
| **Restore** default | withdraw the row, reset, reload | 26/26 pass |
| **Version update** | publish v2, `refresh()` | pass — bound signals update in place |
| **Cache stability** | `refresh()` at unchanged version | pass — same array identity, no consumer re-render |
| **Runtime propagation** | signal held before the change | pass — the same instance yields new values |
| **Selective fetch** | one configured category | pass — one request, not one per known catalog |

Categories covered: AuditType, AuditRecurrence, AuditSessionStatus, AuditSessionResult,
ScheduledAuditStatus, ObservationType, ReportPeriod, FindingStatus, FindingSource,
FindingClassification, FindingSeverity, FindingAttachmentType, FindingQuickFilter,
PromotionReadiness, DeficiencyStatus, DeficiencySeverity, DeficiencyGrouping, RiskLevel,
RootCauseCategory, CapaActionStatus, EffectivenessReviewStatus, EffectivenessReviewSchedule,
MaturityStage, Department, SlaStatus, AssignmentConfidence. Plus AuditWorkflowStage and
AuditWorkflowSurface, validated in §4.

**Write / Override / Restore — a scope correction.** The brief lists these as verified capabilities.
They are verified *as the client consumes them*: a tenant override arriving from the API takes
effect, and withdrawing it restores the platform default. They are **not** exercisable from the
Angular client, which has no write path — `ConfigurationApiService` exposes only reads. The write,
override and restore endpoints exist on `PlatformConfigurationAppService`; no UI calls them. See
Production Readiness, Risk 1.

---

## 4. Phase 2 — lifecycle simulation

Eight checkpoints driven as the facts a workspace reports, asserted against the engine.

| Checkpoint | Current stage | Result |
| --- | --- | --- |
| Planning | `planning` | pass |
| Execution | `execution` | pass |
| Evidence | `evidence` | pass |
| Findings | `findings` | pass |
| CAPA | `capa` | pass |
| Verification | `reports` | pass — see note |
| Closure | `reports` | pass |
| Reports | none (complete) | pass |

Plus **monotonicity**: lifecycle position never regresses as work accumulates. A lifecycle that moved
backwards under more evidence would be worse than none, and nothing previously asserted it.

**Verification and Closure resolve to `reports`, and that is correct for the platform default.**
Neither is a stage in the seeded lifecycle; they are rules inside CAPA and Reports. This was reported
in Wave 2 and is unchanged. Phase 4 demonstrates adding Verification as a real stage through
configuration alone.

**All six audit types run the same lifecycle**, asserted for InternalAudit, DepartmentAudit,
MockAudit, FocusedAudit, Surveillance and FollowUpAudit. The types differ in *attributes* —
schedulability, session-startability, surveillance membership — not in the stages they traverse. The
assertion exists to stop a later change making the lifecycle type-dependent in code rather than in
configuration.

---

## 5. Phase 3 — tenant isolation matrix

| Property | Case | Result |
| --- | --- | --- |
| Label isolation | A's renamed type does not survive into B | pass |
| Workflow isolation | A's custom lifecycle does not survive into B | pass |
| Version isolation | two tenants whose version hashes collide | pass |
| Cache isolation | load-once guard cleared on reset | pass |
| Default window | between sign-out and sign-in, platform defaults show | pass |
| Permission isolation | A's surface restrictions do not survive into B | pass (via workflow isolation) |

The colliding-version case is worth calling out: versions are per-tenant content hashes and can
coincide. A version check that survived the reset would treat the incoming tenant's catalog as
unchanged and keep the outgoing tenant's. `reset()` clears the version alongside the values, so this
holds — but it holds by construction rather than by accident, and now has a test.

---

## 6. What a signed-off browser session must still cover

The following are the specific things this method cannot reach. They are not "run the tests again in
a browser"; they are the gaps.

1. **Rendered propagation.** That a configured label reaches the DOM on each screen — grids, filter
   dropdowns, lifecycle rails, dashboard breakdowns, context drawer.
2. **The status bar under real facts.** Only one page currently reports `lifecycleFacts`
   (`auditor-session-workspace-page`). Everywhere else the bar falls back to positional progress.
   Worth confirming that reads correctly, and worth deciding whether more pages should report facts.
3. **Route guard behaviour** with real role claims, including the alias forms
   (`normalizeRoleId`) that the guard handles but no test exercises against real tokens.
4. **The hospital-switch reload against a live Gateway** — specifically that `reset()` + `load()`
   after token reissue resolves the new organization. The ordering is correct by construction; only
   a live token proves it.
5. **Performance.** Configuration is one request plus one per configured category at sign-in. With
   28 categories configured that is 29 requests. Not measured here.

---

## 7. Regression

| Check | Before this wave | After |
| --- | --- | --- |
| Unit tests | 220 (1 pre-existing failure) | **344** (same 1 failure) |
| Wave 1 + 2 specs | passing | passing, unchanged |
| `tsc --noEmit` | clean | clean |
| AOT production build | succeeds | succeeds |
| ESLint (audit + auditor + core/configuration) | 7 errors | **5** errors |

The two removed lint errors were unused imports left behind by Wave 1's cleanup
(`RootCauseCategory`, `DeficiencyCountByLabel`) — my own leftovers, found by this audit and removed.

**No Critical or High defects remain open.** One Medium (DEF-CFG-004) is open with a recommended
fix; three Low technical-debt items are registered.

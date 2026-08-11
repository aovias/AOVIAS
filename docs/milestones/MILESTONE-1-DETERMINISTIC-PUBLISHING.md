# Milestone 1 — Deterministic Publishing
## Engineering Specification & Publishing Readiness Review

| | |
|---|---|
| **Status** | Draft for review — no implementation until approved |
| **Author** | Principal Software Architect review (AOVIAS Platform) |
| **Date** | 2026-07-05 |
| **Governs** | All subsequent Milestone 1 implementation work |
| **Evaluated against** | Target state: zero-manual-operation deployment onto a clean Ubuntu VPS |

---

## 1. Executive Summary

Publishing is the root dependency of the AOVIAS roadmap: every later stage (registration, readiness, deploy, certification, rollback) consumes what publishing produces. This review traced the entire pipeline in code and found that **the architecture is fundamentally sound and already substantially deterministic** — a canonical 13-stage pipeline, contract-driven compose generation with double validation, checksum + certificate verification, deterministic fingerprints, and a golden-package regression gate all exist and are wired in.

However, publishing is **not yet provably deterministic**, for five concrete reasons found in the code:

1. **The deterministic fingerprint is unreliable for its core artifact.** `service-image.tar` is hashed through a UTF-8 text normalizer (lossy for binary data), and Docker image tars are not byte-stable across rebuilds — the Runtime Agent image is *intentionally* unique per build (`SOURCE_REVISION`, `BUILD_UTC`, `--no-cache`). The fingerprint therefore changes on every Runtime Agent publish, which forces the golden-package gate to either block or be re-approved every time, training operators to rubber-stamp it.
2. **Image staleness protection covers only one of ten services.** Source-revision verification (`RuntimeAgentPackageVerifier`) exists only for `runtime-agent`. The other nine services rely on the Docker layer cache with a constant default tag (`1.0.0`) and no content verification.
3. **A dead 320-line legacy pipeline still ships inside `PublishingPipeline`**, duplicating the live stage executor's logic. Parallel implementations are exactly what this milestone must eliminate.
4. **Dependency and service knowledge is hardcoded in publishing code, bypassing contracts.** Dependencies come from a `switch` statement; `manifest.json` always declares zero dependencies; the demo bundle service list is duplicated in two classes; compose bakes concrete profile paths despite claiming to be environment-neutral.
5. **The release library has two parallel import implementations with different layouts and different gates** (CSM flat copy vs. category-folder organizer), and the pipeline's own ReleaseLibrary stage is a no-op.

Every previously reported publishing issue was re-verified against the current implementation (§7): stale compose, cached `service-image.tar`, and missing bind mounts are **Resolved with regression protection**; stale Docker images and environment drift are **Partially Resolved**; missing infrastructure contracts **Still Exist** (confirmed by `Aovias.Deployment.Contracts/docs/INFRASTRUCTURE_GAP_ANALYSIS.md`).

Milestone 1 therefore has a narrow, high-leverage scope: make the fingerprint truthful, extend build-identity verification to all services, collapse to one pipeline and one release-library import, move all remaining hardcoded knowledge into contracts, and lock everything behind a reproducibility regression suite. No new subsystems are required.

---

## 2. Publishing Architecture Assessment (Deliverable 1)

### 2.1 Entry points and control flow

There are two operator-facing publish flows, both converging on the same pipeline:

- **Single service:** CSM → `Aovias.Publishing.SDK` → `IPublishingPipeline.ExecuteAsync` → `PublishingPipelineOrchestrator.ExecuteAsync` (`PublishingPipeline.cs:130-134` delegates unconditionally to the orchestrator).
- **Whole solution:** CSM (`PublishingWorkspaceService.PublishSolutionAsync`) → `SolutionPublishingOrchestrator.PublishSolutionAsync`, which runs preflight, discovers deployable projects, publishes them via `Parallel.ForEachAsync`, then runs **sequential deferred DRV**, release-library sync, solution manifest + demo bundle generation, platform deployment simulation, and package inspection (`SolutionPublishingOrchestrator.cs:61-280`).

### 2.2 Canonical stage order (the live pipeline)

`PublishingStageExecutor.CanonicalOrder` (`PublishingStageExecutor.cs:79-94`):

```
Discovery → Preflight → Restore → Build → DockerBuild
→ ConfigurationGeneration → ContractGeneration → ComposeGeneration
→ PackageGeneration → PackageVerification
→ DeploymentReadinessValidation (DRV) → ReleaseLibrary → DeploymentCertification
```

This matches the required trace (Solution Publish → Project Discovery → Build → Docker Build → Configuration Generation → Contract Generation → Compose Generation → Package Verification → Deployment Package → Release Library) with one important nuance: **compose generation correctly runs *after* contract generation and hard-fails without contracts** (`Generators.cs:270-272`), so the contract→compose ordering is enforced, not conventional.

### 2.3 Component & artifact ownership

| Pipeline stage | Owning component | Artifacts produced |
|---|---|---|
| Discovery | `ProjectTypeProviderRegistry` + providers (DotNet / Angular / Static) | `ProjectDiscoveryResult` |
| Preflight (solution) | `SolutionPublishingPreflightService` (995 lines: paths, profiles, release library, DRV config, Dockerfiles, dotnet, Docker, Angular, solution build) | `SolutionPublishingPreflightResult` |
| Restore / Build | Project type provider (`DotNetProjectTypeProvider` etc.) | publish output in session workspace |
| Docker Build | `DockerfileGenerator`, `RuntimeAgentDockerContextStager`, `IDockerCliExecutor`; `RuntimeAgentPackageVerifier` (runtime-agent only) | Docker image `aovias/{serviceId}:{version}`, `service-image.tar` |
| Configuration Generation | `DeployManifestGenerator`, `HealthDefinitionGenerator`, `ConfigTemplateGenerator`, `EnvironmentFileGenerator` | `manifest.json`, `health.json`, `config-template.json`, `.env.template` |
| Contract Generation | `ServiceContractGenerator` → `ServiceContractBuilder` (Aovias.Deployment.Contracts) | 12 contract JSON files (service manifest, deployment, filesystem, certificate, database, secret, configuration, dependency, health, network, volume, feature) + `runtime-capabilities.json` |
| Compose Generation | `ComposeGenerator` → `ComposeDocumentBuilder` + `ComposeMountBuilder` + `ComposeYamlSerializer`; validated by `ComposeMountValidator` (Aovias.Deployment.Contracts) | `docker-compose.yml` |
| Package Generation | `DeployPackageBuilder` (+ `PublishingStagingArtifacts` for deferred DRV) | `checksums.json`, interim `.aovdeploy` |
| Package Verification | `PackageVerifier` (wraps `DeployPackageVerifier`, adds contract validation + `DeterministicPackageFingerprint` + `GoldenPackageRepository` comparison) | `PackageVerificationReport` |
| DRV | `Aovias.DeploymentValidation` stages: Package → Compatibility → Docker Compose → Bootstrap → Configuration → Runtime → Health | `DeploymentReadinessReport`, DRV log |
| Deployment Certification | `DeployPackageBuilder.FinalizeAsync` + `PackageVerifier` + `GoldenPackageRepository.StoreGoldenAsync` | `deployment-readiness.json` certificate, final `.aovdeploy`, `*.publishing-report.json` sibling, golden package |
| Release Library | `ReleaseLibraryOrganizer` (solution flow), `PublishingWorkspaceService.AddToReleaseLibraryAsync` (CSM flow) — pipeline stage itself is a **no-op** (`PublishingStageExecutor.cs:528-541`) | categorized/flat library copies, `solution-deployment.json`, `DemoDeployment.zip` |
| Workspace lifecycle | `PublishingWorkspaceManager` (session GUID workspaces, 2-day stale cleanup, replay artifact manifests) | session workspace, stage-artifact manifests, logs |
| Reporting | `PublishingReportBuilder`, `PublishingReliabilityReportBuilder`, `QualityGateEvaluator` | publishing report, reliability report, quality gates |

### 2.4 Dependency graph

```
Solution Publish (SolutionPublishingOrchestrator)
│  preflight ── SolutionPublishingPreflightService
▼
Project Discovery (ProjectTypeProviderRegistry + providers)
▼
Build (provider Validate/Restore/Build/Publish/InspectConfiguration)
▼
Docker Build (DockerfileGenerator → docker build → docker save → service-image.tar)
│  runtime-agent only: RuntimeAgentDockerContextStager staging,
│  SOURCE_REVISION build-arg, RuntimeAgentPackageVerifier image check
▼
Configuration Generation (manifest.json, health.json, config-template.json, .env.template)
▼
Contract Generation (ServiceContractGenerator → ServiceDeploymentBundle, 12 contract files)
▼
Compose Generation (ComposeDocumentBuilder + ComposeMountBuilder ← VolumeContract)
│  validated twice: object model + serialized YAML (ComposeMountValidator)
▼
Package Generation (DeployPackageBuilder: staging → checksums.json → interim .aovdeploy)
│  ComposeMountValidator re-run against staged compose + contracts
▼
Package Verification (PackageVerifier: required files, checksums, contract validation,
│                     deterministic fingerprint, golden comparison)
▼
DRV (7 stages incl. container startup + health in temp workspace)
▼
Deployment Certification (deployment-readiness.json + final verify + golden store)
▼
Deployment Package (.aovdeploy in output folder + publishing-report sibling)
▼
Release Library (organizer copy, solution-deployment.json, DemoDeployment.zip,
                 platform simulation, SolutionPackageInspector)
```

Cross-assembly dependencies: `Publishing.Core` → `Aovias.Configuration` (settings, `DeploymentProfile`, `DeploymentPathResolver`), `Aovias.Deployment.Contracts` (contract builders, `ComposeMountValidator`, package layout), `Aovias.DeploymentValidation` (DRV). Consumers of the package: `Aovias.PackageBuilder` / `Aovias.ReleaseManager` (upload/validate), Runtime Agent (deploy). Any package-layout change is therefore a **cross-repo contract change**.

---

## 3. Determinism Assessment (Deliverable 2)

**Verdict: publishing is *validated* but not *reproducible*.** Generated text artifacts (compose, contracts, env, config template) are deterministic for fixed inputs; the binary image artifact and the fingerprint that is supposed to prove determinism are not.

### 3.1 Sources of non-determinism

| # | Source | Root cause | Current implementation | Remaining risk | Recommendation |
|---|--------|-----------|------------------------|----------------|----------------|
| N1 | `service-image.tar` bytes differ across rebuilds | `docker save` output embeds image ID / layer metadata; Runtime Agent builds are deliberately unique (`SOURCE_REVISION` = new per build, `BUILD_UTC=DateTimeOffset.UtcNow`, `noCache=true`, `PublishingStageExecutor.cs:290-299`) | Tar is **included** in `DeterministicPackageFingerprint` (only `metadata.json`, certificate, publishing report are volatile — `DeterministicPackageFingerprint.cs:16-21`) | Runtime Agent fingerprint changes every publish → golden gate (`RunPackageVerificationAsync` fails on `RequiresApproval`, `PublishingStageExecutor.cs:456-460`) either blocks every publish or is habitually re-approved, destroying its regression value | Fingerprint the **image identity** (config digest / layer digests via `docker image inspect`) recorded in `manifest.json`, and exclude raw tar bytes. Derive Runtime Agent `SOURCE_REVISION` from **source content hash** (or git SHA), not per-invocation randomness, so unchanged source ⇒ unchanged revision |
| N2 | Binary content hashed as UTF-8 text | `DeterministicPackageFingerprint.NormalizeStream` reads **every** entry, including the multi-hundred-MB image tar, through `StreamReader(UTF8)` (`DeterministicPackageFingerprint.cs:58-76`) | Invalid byte sequences collapse to U+FFFD; hash is lossy (distinct tars can hash identically) and memory-hungry (`ToArray()` of full tar) | Fingerprint can silently miss real changes; large-package OOM risk | Hash raw bytes for non-text entries; stream the hash (no full-buffer `ToArray()`) |
| N3 | Docker layer cache for the nine non-runtime-agent services | `noCache` and revision verification are runtime-agent-only | Layer cache is content-keyed (usually safe), but base image tags in Dockerfile templates are not digest-pinned, and no post-build content verification exists for these services | A floating base tag or poisoned cache produces a stale image that passes all checks — the exact class of the historical "stale Docker image" bug, still open for 9/10 services | Generalize the build-manifest scheme: inject `aovias-build-manifest.json` (source hash, build args) into **every** image; verify post-build like `RuntimeAgentPackageVerifier`; pin base images by digest in templates |
| N4 | Package archive bytes | `ZipFile.CreateFromDirectory` embeds file timestamps (`DeployPackageBuilder.cs:167-175`) | Handled: fingerprint hashes entry *content*, not zip bytes; `checksums.json` covers per-file content | Low — but `.aovdeploy` files themselves can never be byte-compared | Acceptable; optionally adopt a deterministic zip writer (fixed timestamps, ordered entries) to make packages byte-reproducible |
| N5 | Timestamps inside artifacts | `metadata.json.CreatedUtc`, certificate `ValidatedUtc`/`ValidationId=Guid`, publishing report | Correctly declared volatile in the fingerprint; YAML/JSON timestamp regex-normalization for other files | Low — by design | Keep; document the volatile set as a versioned policy |
| N6 | Version fallback `"1.0.0"` duplicated | Literal repeated in `PublishingStageExecutor.cs:283,392`, `DeployManifestGenerator`, `ServiceContractGenerator`, `PublishingStagingArtifacts` | Same value everywhere today | Divergence risk on future change; constant tag also means `aovias/{service}:1.0.0` is silently overwritten in the local daemon on every publish | Centralize in one constant; include version in image tag policy review (M1) and session-scoped build tags if concurrent same-service publishes are ever allowed |
| N7 | Profile baked into compose | `ComposeDocumentBuilder.cs:46-52` writes **concrete** `profile.ConfigRoot`, `profile.LogsRoot`, `profile.DeploymentsRoot` values while other keys use `${AOVIAS_*}` references — contradicting its own doc-comment ("emitted as environment references") | `.env.template` also carries concrete values from the publisher's active profile (`EnvironmentFileGenerator`) | Same source + different publisher profile ⇒ different package; compose is not environment-neutral; drift between compose-baked values and `.env` values is possible | Emit **all** host-dependent values as `${VAR}` references; the `.env` (validated at deploy time) becomes the single source of environment values |
| N8 | Live settings snapshot | `IOptionsMonitor<PublishingSettings>.CurrentValue` read repeatedly across stages | Settings hot-reload mid-publish would change behavior between stages | Low likelihood, hard to debug | Capture one settings snapshot per session at pipeline start |
| N9 | Stage retry via Polly (`Handle<Exception>`, orchestrator `CreateRetryPolicy`) re-runs whole stages | Stages are mostly overwrite-idempotent | Docker build retry after partial state is safe today; future stages may not be | Document idempotency as a stage contract requirement |
| N10 | Stage-artifact reuse (`ReuseSuccessfulArtifacts`) | `TryLoadStageArtifacts` restores `docker-image.tar` / interim package **with no input fingerprint check** (`PublishingPipelineOrchestrator.cs:74-90`) | In practice inert: every run creates a fresh GUID session (`PublishingWorkspaceManager.CreateSession:27`), so nothing is ever reused cross-run | Half-built mechanism; if ever activated it becomes a first-class stale-artifact vector | Either delete, or complete it with input-fingerprint keying (M1 recommends **delete**; replay belongs to a future milestone with proper content addressing) |

### 3.2 Stale-artifact controls that already work (verified)

- Compose cannot go stale: regenerated every publish, token-checked (`EnsureRequiredTokens`, rejects obsolete `version:` field), mount-validated against contracts twice at generation (`Generators.cs:277-293`), again at package generation (`PublishingStageExecutor.cs:408-422`), and again in DRV's Docker Compose stage. Determinism regression test exists (`RegressionTests.ComposeGenerator_is_deterministic_and_drops_version_field`).
- `service-image.tar` cannot be silently reused from a stale package: deferred-DRV flow protects the tar in a `protected/` copy and **throws** "Rebuild the Runtime Agent image instead of reusing a stale deployment package" if it is missing (`PublishingStagingArtifacts.cs:22-40`).
- Workspaces are session-isolated (GUID) with 2-day stale cleanup (`CleanStaleWorkspaces`, `PublishingPipelineOrchestrator.cs:57`); DRV temp dirs older than 6h are purged (`RecoverWorkspace`).
- Output package is deleted before re-creation; release library copies are overwrite-synced after DRV (`SyncReleaseLibrary`).

---

## 4. Artifact Dependency Matrix (Deliverable 3)

| Artifact | Generated By | Consumed By | Validation | Deterministic |
|---|---|---|---|---|
| `manifest.json` | `DeployManifestGenerator` | Runtime Agent, DRV, `DeployPackageReader`, compose builder | Required-file + checksum + non-empty service (`DeployPackageVerifier`) | ✅ for fixed inputs — but `Dependencies` is hardcoded `[]` (`Generators.cs:37`) |
| `health.json` | `HealthDefinitionGenerator` | DRV Health stage, contract builder | Required + checksum + endpoint non-empty | ✅ |
| `config-template.json` | `ConfigTemplateGenerator` (flattens appsettings, placeholders secrets, drops bootstrap keys and `.Development.json`) | Runtime Agent config provisioning, DRV Configuration stage | Required + checksum; `DeploymentArtifactSanitizer` | ✅ |
| `.env.template` | `EnvironmentFileGenerator` from typed `DeploymentProfile` | Deploy-time `.env` finalization, DRV, `EnvTemplateValidationRules` | Required + checksum; env-template rules in DRV | ✅ per profile — profile itself is an input (see N7) |
| `docker-compose.yml` | `ComposeGenerator` (schema-driven object model, YamlDotNet) | Runtime Agent deploy, DRV Compose/Runtime stages | `ComposeMountValidator` ×3 + `EnsureRequiredTokens` + regression test | ✅ per profile (N7: mixed baked/`${}` values) |
| 12 contract files (`*.contract.json`, `service-manifest.json`) | `ServiceContractGenerator` → `ServiceContractBuilder` | Compose generation (volume), DRV, Runtime Agent provisioning pipeline | `ContractValidator.Validate` + per-file checksum enforcement (`PackageVerifier.cs:110-116`) | ✅ — but dependency data originates from a hardcoded switch (§5) |
| `runtime-capabilities.json` | contract bundle IO | Runtime Agent | checksum | ✅ |
| `service-image.tar` | `docker save` after `docker build` | DRV Compatibility/Runtime stages, Runtime Agent deploy | Runtime-agent only: `RuntimeAgentPackageVerifier` (build manifest, SOURCE_REVISION, pathPolicyVersion ≥ 2, deploymentApi, signalRDiscovery) | ❌ (N1/N2/N3) |
| `checksums.json` | `DeployPackageBuilder.WriteChecksums` (SHA-256 per staged file, written at interim **and** rewritten at finalize) | `DeployPackageVerifier.VerifyChecksums`, Runtime Agent | Self-validating | ✅ mechanism; inherits tar non-determinism |
| `metadata.json` | Stage executor (`CreatedUtc = UtcNow`) | Reader, verifier (publisher non-empty) | checksum | ❌ by design → volatile in fingerprint ✅ |
| `deployment-readiness.json` (certificate) | `DeployPackageBuilder.FinalizeAsync` (Guid ValidationId, ValidatedUtc, DRV version, package checksum, stage results) | Release library gate (CSM), Runtime Agent, final verification (`expectCertificate: true`) | Final `PackageVerifier` pass | ❌ by design → volatile ✅ |
| `*.publishing-report.json` (sibling + in-package) | `PublishingReportBuilder` | CSM dashboards | none (best-effort) | ❌ by design → volatile ✅ |
| Interim / final `.aovdeploy` | `DeployPackageBuilder` | DRV, ReleaseManager, Runtime Agent | Full verification chain | Content ✅ / bytes ❌ (N4) |
| Golden package + `golden-metadata.json` | `GoldenPackageRepository.StoreGoldenAsync` (on certification) | `CompareAsync` during next publish's verification | Fingerprint equality | Gate is only as good as the fingerprint (N1/N2) |
| `solution-deployment.json` | `SolutionBundleBuilder` | Runtime Agent platform deploy, `SolutionPackageInspector`, platform simulation | Inspector: manifest present, services deployment-ready, packages exist, per-package verify, demo-set completeness | ✅ |
| `DemoDeployment.zip` | `SolutionBundleBuilder` | Demo distribution | Inspector | ✅ content-wise |

---

## 5. Contract Consistency Review (Deliverable 4)

**Required flow:** Service Contract → Infrastructure Contract → Volume Contract → Compose Generation → Deployment Package.

**What is enforced (evidence):**

- Compose **cannot** be generated without contracts — hard `InvalidOperationException` (`Generators.cs:270-272`).
- Compose volume mounts are built **exclusively** from `VolumeContract` (`ComposeMountBuilder` throws if the contract declares no mounts or duplicates container paths) and validated against Volume + Filesystem contracts three times (generation ×2, package generation ×1) plus DRV.
- Contract files are checksummed and structurally validated in package verification (`PackageVerifier.VerifyContracts`).

**Where publishing bypasses contracts (evidence):**

| # | Bypass | Evidence |
|---|---|---|
| C1 | **Service dependencies are code, not contract input.** `DeployableProjectNaming.GetDependencies` is a hardcoded `switch` over service IDs (`DeployableProjectNaming.cs:66-79`); `manifest.json` simultaneously hardcodes `Dependencies = []` (`Generators.cs:37`). Two sources of truth, one of them empty. | Confirms gap-analysis finding "Dependencies: flat list… order only in publishing" |
| C2 | **Compose environment block is not contract-driven.** `ComposeDocumentBuilder` injects env vars directly (incl. concrete `profile.ConfigRoot`/`LogsRoot`/`DeploymentsRoot` and service-agnostic `COMPLIANCE_LOG_ROOT`) rather than deriving from the Configuration/Filesystem contracts (`ComposeDocumentBuilder.cs:44-52`). | |
| C3 | **Ports and healthcheck come from discovery, not the Network/Health contracts.** Same values today, but the contract is generated *from* the same discovery data and then not used as compose input. | `ComposeDocumentBuilder.cs:28-42` |
| C4 | **No unified `infrastructure.contract.json`.** Contracts ship as 12 separate files; secrets contracts are incomplete (connection strings, RabbitMQ) per the audited gap analysis. | `INFRASTRUCTURE_GAP_ANALYSIS.md` §Cross-Cutting |
| C5 | **Demo bundle membership is hardcoded twice** (`SolutionPublishingOrchestrator.DemoBundleServiceIds`, `SolutionPackageInspector`), excluding `knowledge-base`/`outbox-worker` with no contract-level justification. | `SolutionPublishingOrchestrator.cs:18-28` |
| C6 | **Startup order lives on `DeployableProject` discovery metadata**, not in the dependency contract consumed at deploy time. | `SolutionPublishingOrchestrator.cs:154-157` |

Conclusion: the Volume/Filesystem path is genuinely contract-driven end-to-end; the Dependency, Configuration, Network, and Infrastructure paths are **generated but bypassed** — contracts are emitted as *outputs* yet not used as *inputs* to compose or ordering.

---

## 6. Previous Issue Verification Matrix (Deliverable 5)

| Issue | Classification | Evidence |
|---|---|---|
| Stale Docker images | **Partially Resolved** | Runtime Agent: resolved — `--no-cache`, unique `SOURCE_REVISION`, post-build in-image manifest verification incl. explicit "stale cached layers" and `pathPolicyVersion` checks (`RuntimeAgentPackageVerifier.cs:70-90`). Other 9 services: no revision verification, cache enabled, constant default tag (N3) |
| Stale compose | **Resolved** | Regenerated per publish from contracts; triple mount validation; required-token + obsolete-`version:` rejection; determinism regression test (§3.2) |
| Stale deployment package | **Resolved** (with caveat) | Output deleted & recreated (`DeployPackageBuilder.cs:88-93`); final verification requires certificate; golden fingerprint gate. Caveat: golden gate value degraded by N1 |
| Cached `service-image.tar` | **Resolved** | Protected-copy mechanism + hard failure with recovery text (`PublishingStagingArtifacts.cs:22-40`); `PublishingStagingArtifactsTests` exist |
| Missing deployment bind mounts | **Resolved** | `ComposeMountBuilder` throws on empty/duplicate mounts; `ComposeMountValidator` at 4 points; `ComposeMountBuilderTests`, `ComposeMountValidatorTests` |
| Environment variable drift | **Partially Resolved** | `.env.template` generated from strongly typed `DeploymentProfile.ToEnvironmentVariables()` (no free-form placeholders); `EnvironmentComparison` regression test; DRV env-template rules. Open: compose bakes concrete profile paths alongside `${}` refs (N7) — dual source of truth remains |
| Missing infrastructure contracts | **Still Exists** | Gap analysis is current: no aggregate `infrastructure.contract.json`, secret contracts incomplete (connection strings, RabbitMQ required-ness), providers/network not contract-declared. Publishing emits 12 files but not the unified contract |
| Package verification | **Resolved** | Required files, per-file SHA-256, contract structural validation, contract-checksum presence, certificate expectation, deterministic fingerprint + golden comparison, final re-verify after certification |
| Release library inconsistency | **Partially Resolved** | Solution flow: post-DRV `SyncReleaseLibrary` + `SolutionPackageInspector` (manifest/bundle presence, per-package verify, demo-set completeness) + platform simulation. Open: two import implementations with different layouts and gates (pipeline stage is a no-op; CSM copies flat with cert gate; organizer copies to category folder without its own gate); versionless filenames mean the library holds only "latest" with no history |
| Stale publishing artifacts (workspaces) | **Resolved** | GUID session isolation, 2-day workspace cleanup, 6-hour DRV temp cleanup, `.tmp` recovery deletion |
| Runtime Agent packaging / path policy | **Resolved** | Monorepo Docker context staged & validated (`RuntimeAgentDockerContextStager.ValidateStagedContext`), `pathPolicyVersion >= 2` gate, `RuntimeAgentDockerContextStagerTests`, `RuntimeAgentComposeGenerationTests` |
| Deployment storage / path resolution | **Unable to Verify (in publishing scope)** | Owned by `Aovias.Configuration` (`DeploymentPathResolver`, storage diagnostics) and Runtime Agent provisioning; publishing consumes resolved profiles only. Verify in Milestone 2 |

---

## 7. Technical Debt Report (Deliverable 6)

| # | Item | Evidence | Class | Disposition |
|---|---|---|---|---|
| D1 | `PublishingPipeline.ExecuteLegacyAsync` — full ~320-line legacy pipeline, private and **never referenced** (repo-wide grep: definition only) | `PublishingPipeline.cs:136-453` | Dead parallel implementation | **Delete** |
| D2 | Duplicated deferred-package completion (`CompleteDeferredPackageAsync` exists in both `PublishingPipeline` and `PublishingPipelineOrchestrator`) and duplicated `EmitPublishingReportSibling` | `PublishingPipeline.cs:520-565,717-740` vs `PublishingPipelineOrchestrator.cs:200-249,439-460` | Duplicate implementation | **Merge** into orchestrator; `PublishingPipeline` becomes a thin façade or is removed in favor of `IPublishingPipelineOrchestrator` |
| D3 | `PublishingPipelineOrchestrator.ShouldSkipStage` — always returns `false`, unreferenced logic | `PublishingPipelineOrchestrator.cs:299-303` | Dead code | **Delete** |
| D4 | Stage-artifact reuse machinery (`ReuseSuccessfulArtifacts`, `TryLoadStageArtifacts`, `ApplyArtifacts` with fragile `Artifacts.Zip(Outputs)` key/value pairing) — inert because sessions are always fresh GUIDs, unsafe if activated (no input fingerprint) | `PublishingPipelineOrchestrator.cs:74-90,305-319`; `PublishingWorkspaceManager.cs:24-77` | Half-built feature / latent stale-artifact vector | **Delete** in M1; re-introduce later only with content-addressed inputs |
| D5 | Two verifier layers: `IDeployPackageVerifier` (base) + `IPackageVerifier` (extended). The dead legacy path uses the base one directly; live path uses the wrapper | `PackageVerifier.cs`, `DeployPackageVerifier.cs` | Layering residue | **Refactor**: keep both types but make `IPackageVerifier` the only pipeline-facing interface (falls out of D1/D2) |
| D6 | Hardcoded service knowledge in publishing code: dependency `switch` (`DeployableProjectNaming.cs:66-79`), `DemoBundleServiceIds` duplicated in orchestrator + inspector, `COMPLIANCE_LOG_ROOT` in the generic compose builder, empty `manifest.Dependencies` | §5 C1/C2/C5 | Contract bypass | **Refactor**: move to per-project deployment metadata consumed by contract generation (see spec §8.4) |
| D7 | Concrete profile paths baked into compose despite env-reference design intent | `ComposeDocumentBuilder.cs:46-52` | Temporary fix drift | **Refactor** (N7) |
| D8 | `"1.0.0"` version fallback repeated in ≥5 locations | §3.1 N6 | Hardcoded values | **Merge** into a single constant/policy |
| D9 | Release library: two import implementations (CSM flat + cert-gated vs organizer categorized + caller-gated), pipeline stage no-op, versionless filenames (`PublishingImageNaming.GetPackageFileName(serviceId)`) with an unused versioned overload | §5 C5, §6; `ReleaseLibraryOrganizer.cs`, `PublishingWorkspaceService.cs:85-122`, `PublishingStageExecutor.cs:528-541` | Parallel implementations | **Merge** into one library service (single layout, single certification gate, versioned retention) |
| D10 | Legacy staged source trees under `aovias-config/storage/local/publishing/temp/sessions/**` retained on disk (full monorepo copies from Runtime Agent context staging) | observed in repo | Storage debt | **Keep mechanism, tighten cleanup** (staging is required; ensure session cleanup always runs, incl. deferred-DRV failure paths) |
| D11 | Heuristic status mapping via string matching (`MapProjectStatus` `ContainsAny("Build"…)`, `Failed = message.Contains("fail")`) | `SolutionPublishingOrchestrator.cs:537-558` | Fragile UI plumbing | **Refactor** opportunistically (use stage IDs/states, not strings); not a determinism risk |
| D12 | Retry policy re-runs whole stages on any exception without an idempotency contract | `PublishingPipelineOrchestrator.cs:368-378` | Design gap | **Keep** with documented per-stage idempotency requirement (N9) |

No obsolete pipeline *stages* were found: all 13 canonical stages execute meaningful work except ReleaseLibrary (no-op, D9) and the skip-eligible certification path.

---

## 8. Engineering Specification — Milestone 1 (Deliverable 7)

### 8.1 Objective

Make publishing **provably deterministic and fully contract-originated**: identical source + profile inputs produce packages with identical deterministic fingerprints for **all ten services**; every artifact in the package originates from a contract or a generator whose inputs are contracts; exactly one implementation exists per pipeline concern; publishing either completes with a verified, certified, golden-comparable package or fails **before** packaging with deterministic guidance.

### 8.2 Dependencies (existing systems this milestone builds on — all verified present)

`PublishingPipelineOrchestrator` + `PublishingStageExecutor` (live pipeline), `ServiceContractBuilder`/`ContractValidator`/`ComposeMountValidator` (Aovias.Deployment.Contracts), `DeterministicPackageFingerprint` + `GoldenPackageRepository`, `RuntimeAgentPackageVerifier` (template for generalization), `PublishingWorkspaceManager`, DRV (Aovias.DeploymentValidation), `SolutionPublishingPreflightService`, `SolutionPackageInspector`, `DeploymentProfile`/`DeploymentPathResolver` (Aovias.Configuration). **No unfinished architecture is required**: every work item below completes or corrects an existing component.

### 8.3 Scope

**In scope**

1. Fingerprint correctness (N1, N2): raw-byte streaming hashes; image identity by digest; versioned volatile-entry policy.
2. Universal build-identity verification (N3): build manifest in every image; content-derived `SOURCE_REVISION`; digest-pinned base images in Dockerfile templates.
3. Single pipeline (D1–D5): delete legacy path, dead code, inert reuse machinery; one verifier entry point.
4. Contract-originated generation (C1–C6, D6–D8): per-project deployment metadata → dependency contract → manifest + compose + startup order; compose env/ports/health from contracts; all host values as `${}` references; aggregate `infrastructure.contract.json` emission with completed secret declarations (gap-analysis P0 items that are *publishing outputs*).
5. One release library (D9): single import service, versioned filenames + `latest` manifest, certification gate, atomic copy.
6. Golden-package approval workflow: fingerprint mismatch becomes an explicit, recorded approve/reject decision (CSM), not a hard pipeline failure that gets scripted around.
7. Regression suite (§10).

**Out of scope (later milestones)**

- Consuming `infrastructure.contract.json` in the Runtime Agent provisioning pipeline (M2).
- VPS bootstrap installer (M3); environment registration gates (M4); deploy/health/certification execution (M5); rollback/history (M6).
- Package format breaking changes beyond additive contract files (Runtime Agent/ReleaseManager readers must keep working; additive-only rule).
- CSM UI redesign; replay/artifact-cache re-introduction; vault-backed secrets.

### 8.4 Target architecture after completion

- **One pipeline**: `IPublishingPipelineOrchestrator` is the only execution path; `PublishingPipeline` façade removed or reduced to delegation-only (no logic).
- **Deterministic identity chain**: source content hash → build manifest (all images) → image digest recorded in `manifest.json` → fingerprint = hash(text artifacts normalized + image digest + contract set) → golden comparison → certification. Unchanged source ⇒ byte-identical fingerprint, including runtime-agent.
- **Contract-originated generation**: per-project `deployment.metadata.json` (or csproj items) declares dependencies, ports, health, demo-bundle membership, startup tier → `ServiceContractGenerator` is the *only* consumer → manifest, compose, solution manifest, and inspector all read the generated contracts. The hardcoded switch and duplicated service lists are deleted.
- **Environment-neutral compose**: zero concrete host paths; every environment-specific value is a `${VAR}` resolved from the deploy-time `.env`; `.env.template` remains the typed, validated template. One source of truth for environment values.
- **Single release library**: `{root}/{category}/{serviceId}-{version}.aovdeploy` + library manifest tracking latest/history; import requires certificate + verification; both CSM and solution flows call the same service; the pipeline ReleaseLibrary stage invokes it (no longer a no-op) or is removed from `CanonicalOrder`.
- **Failure semantics**: every stage failure carries `FailureCategory`, `Recommendation`, `RecoveryAction` (already present) — extended so no failure path can emit a package (packaging stages hard-gate on all upstream results).

### 8.5 Implementation strategy (ordered; each step independently shippable)

1. **Debt removal first** — delete `ExecuteLegacyAsync`, `ShouldSkipStage`, reuse machinery; merge deferred completion + report emission; collapse verifier layering. Zero behavior change intended; existing tests must stay green. *(Unlocks: every later change lands in exactly one place.)*
2. **Fingerprint fix** — raw-byte streaming hash; exclude `service-image.tar` bytes, include image digest(s); publish the volatile-entry policy as `FingerprintPolicy` with version. Re-baseline golden packages once.
3. **Universal build identity** — generalize the build-manifest injection + post-build verification to all providers; derive `SOURCE_REVISION` from source content hash; pin base image digests in templates. Remove runtime-agent special-casing where it becomes generic.
4. **Contract origination** — introduce per-project deployment metadata; route dependencies/ports/health/demo membership through it; populate `manifest.Dependencies` from the dependency contract; delete `GetDependencies` switch + `DemoBundleServiceIds` duplicates; startup order derived from topological sort of the dependency contracts.
5. **Environment-neutral compose** — replace baked profile paths with `${}` references; move `COMPLIANCE_LOG_ROOT` out of the generic builder (contract-declared env); extend `ComposeMountValidator`/DRV assertions to reject concrete host paths in compose.
6. **`infrastructure.contract.json`** — aggregate emission from the existing 12 contracts + completed secret declarations (connection strings required for SQL services, RabbitMQ required for compliance, per gap analysis). Additive to the package layout.
7. **Release library unification** — single import service with versioned retention + library manifest; wire CSM and solution flows to it; make the pipeline stage real or remove it.
8. **Golden approval workflow** — mismatch produces a pending-approval record surfaced in CSM with the diff (which entries changed); approval stores the new golden; rejection fails the publish. No silent overwrite, no habitual failure.
9. **Regression suite** (§10) — added incrementally alongside steps 2–8, plus the double-publish reproducibility test as the milestone gate.

### 8.6 Risks

| Risk | Mitigation |
|---|---|
| Docker builds are not perfectly reproducible even with pinned bases (file timestamps in layers) | Determinism is defined at the **image digest / build-manifest** level, not tar bytes; unchanged source + cached layers ⇒ unchanged digest. `--no-cache` paths accept digest change but verify source hash via build manifest |
| Package layout changes break Runtime Agent / ReleaseManager readers | Additive-only rule in M1; `DeployPackageLayout` shared constants; reader round-trip tests in both repos' suites |
| Golden re-baselining floods approvals right after the fingerprint change | One-time controlled re-baseline step in rollout; approval workflow (step 8) lands before fingerprint enforcement is tightened |
| Removing legacy pipeline breaks an unnoticed consumer | Grep evidence shows no references; guard with compile + full test run in step 1 before any behavioral work |
| Per-project metadata introduces drift with reality (declared vs actual ports/health) | DRV already boots the container and probes health — declared values are runtime-verified per publish |
| Base-image digest pinning ages (security patches) | Pin + scheduled bump policy; preflight warns when pinned digest is older than a threshold |

---

## 9. Definition of Done

Publishing is deterministic. Concretely:

- Publishing the same source tree + profile twice yields **identical deterministic fingerprints for all 10 services**, including runtime-agent.
- Every package artifact originates from contracts or contract-fed generators; no hardcoded service lists, dependencies, or host paths remain in `Aovias.Publishing.*` (verified by test + grep gate).
- No manual corrections: no step in single-service or solution publish requires editing generated files, deleting caches, or re-running with different flags to succeed.
- No stale artifacts: image content verified against source revision for every service; compose/env/contracts regenerated and validated every run; workspace/session hygiene unchanged or better.
- Publishing either succeeds completely (verified + certified + golden-compared package in the release library) or **fails before packaging** with `FailureCategory` + `RecoveryAction`.
- Exactly one implementation exists for: pipeline execution, package verification, release-library import.

## 10. Regression Requirements

Must exist and run in CI (extends `Aovias.Publishing.Core.Tests`):

1. **Reproducibility**: publish fixture service twice → fingerprints identical; mutate one source file → fingerprint changes (both directions).
2. **Fingerprint policy**: volatile entries (metadata, certificate, publishing report) do not affect the fingerprint; binary entries hashed byte-exact (regression for the UTF-8 normalization defect); large-entry streaming (no full-buffer).
3. **Stale image**: image whose build manifest source hash ≠ current source hash fails verification for *any* service (generalizes existing runtime-agent tests).
4. **Stale tar**: existing `PublishingStagingArtifactsTests` retained (protected-tar restoration + hard failure).
5. **Compose**: existing determinism/mount/token tests retained; new test rejecting concrete host paths in compose output.
6. **Contracts**: manifest dependencies == dependency contract == topological startup order; demo bundle membership sourced from metadata; contract checksum enforcement retained.
7. **Env**: `.env.template` ↔ compose variable-reference closure (every `${VAR}` in compose is declared in the template); `EnvironmentComparison` retained.
8. **Release library**: import rejects uncertified/unverified packages; versioned retention + latest manifest correct; solution inspector passes against the unified layout.
9. **Golden gate**: mismatch → pending approval (not silent pass, not unrecoverable fail); approval → new golden stored with audit metadata.
10. **Failure-before-packaging**: induced failure at each pre-packaging stage produces no `.aovdeploy` in the output folder and a populated `RecoveryAction`.

## 11. Exit Criteria (Deliverable 8 — all measurable)

- ✓ 3 consecutive full solution publishes (10/10 services) succeed on a clean machine with zero manual intervention.
- ✓ Double-publish fingerprint equality holds for 10/10 services (CI gate).
- ✓ Package verification, compose validation, contract validation, and DRV pass for 10/10 services.
- ✓ `infrastructure.contract.json` present and schema-valid in 10/10 packages; secret contracts declare connection strings/RabbitMQ per gap analysis P0.
- ✓ `SolutionPackageInspector` passes against the unified release library; library manifest lists version history.
- ✓ Golden comparison runs for 10/10 services with the approval workflow exercised at least once end-to-end.
- ✓ Regression suite §10 fully implemented and green; grep gates confirm 0 references to `ExecuteLegacyAsync`, `GetDependencies` switch, `DemoBundleServiceIds`.
- ✓ Publishing report + reliability report reflect the new verification data (image digest, fingerprint policy version).

## 12. Recommended Implementation Order

`8.5-1 debt removal` → `8.5-2 fingerprint` → `8.5-3 build identity` → `8.5-4 contract origination` → `8.5-5 compose neutrality` → `8.5-6 infrastructure contract` → `8.5-7 release library` → `8.5-8 golden approval` → `8.5-9 regression gate` — each step compiles, tests green, and is independently revertible. Steps 2–3 and 4–5 may be parallelized by different engineers after step 1 lands.

---

## Appendix A — Engineering-principles compliance

- **Verified before recommended**: every claim above cites file/line evidence read from the current tree; no findings were carried over unverified from prior investigations.
- **Architectural over tactical**: fingerprint/identity redesign instead of tar-normalization hacks; contract origination instead of patching individual hardcoded lists.
- **Debt eliminated**: D1–D5, D9 removals are in scope; nothing in this plan adds a parallel implementation.
- **Contract-driven architecture preserved and extended**: the milestone's centerpiece is closing the contract bypasses (§5) rather than adding new generation paths.
- **Measurable movement toward the target state**: after M1, "Publish Solution → Deployment Ready" in the target workflow is trustworthy and repeatable indefinitely — the precondition for Milestones 2–7.


# Milestone M2 — Enterprise Infrastructure Contract Architecture
## Governing Engineering Specification

| | |
|---|---|
| **Status** | Draft for review — architecture & specification milestone, no implementation |
| **Author** | Principal Platform Architect review (AOVIAS Platform) |
| **Date** | 2026-07-05 |
| **References** | `MILESTONE-1-DETERMINISTIC-PUBLISHING.md`, `Aovias.Deployment.Contracts/docs/INFRASTRUCTURE_GAP_ANALYSIS.md`, current `Aovias.Deployment.Contracts` implementation |
| **Design horizon** | PRAMAN, AOVIAS platform, customer/enterprise installs, Kubernetes, multi-cloud, air-gapped |

---

## 1. Executive Summary

The question this milestone answers is whether AOVIAS already has an infrastructure contract architecture or merely infrastructure contract *files*. The evidence supports the second reading, with one important qualification: the **execution machinery is genuinely good** — a generic 9-stage provisioning pipeline (`InfrastructureProvisioningPipeline`) with zero service-specific provisioners, an execution-context abstraction that cleanly separates offline validation (DRV) from live provisioning (Runtime Agent), structured stage results with recovery actions, and a shared `ContractValidator`. That machinery should be retained essentially as-is.

What is missing is the **contract as an authoritative, portable model**. Four findings drive this specification:

1. **The unified infrastructure contract is decorative.** `infrastructure.contract.json` is written into every package (`ServiceDeploymentBundleIO.WriteDirectory`) but is excluded from the checksummed `ContractFiles` list, is never read back by `ReadDirectory`, and the Runtime Agent **rebuilds** the contract in memory from the bundle at deploy time (`ReleaseDeploymentProvider.cs:434`). The artifact that is supposed to be the single source of truth has zero consumers.
2. **The language of infrastructure is C# code, not data.** Every service's infrastructure is defined in a hardcoded `switch` in the 507-line `ServiceContractRegistry`; provider requirements for the `ai` service are hardcoded inside `InfrastructureContractBuilder` itself; the identity service has its own bespoke contract builder. A new deployable service — a customer's service, Redis, RabbitMQ — cannot describe itself without modifying and re-shipping `Aovias.Deployment.Contracts`. This is the single largest violation of the platform vision.
3. **The model is a closed world.** `FeatureContract` and `RuntimeCapabilities` are fixed boolean sets (`RequiresRabbitMq`, `RequiresNginx`, …). Every new infrastructure concept is a breaking model change. There is no requirement/capability abstraction, which is precisely the abstraction Kubernetes, ECS, and air-gapped support will need.
4. **Parallel infrastructure definitions persist outside the contract.** The VPS bootstrap script hardcodes `/opt/aovias/*` paths and a directory list that "matches settings.deployment-storage.json" by comment, not by construction; the Runtime Agent creates release-management folders from settings, not contracts; compose generation injects environment variables from code (M1 finding C2). Filesystem truth currently lives in at least four places.

The specification below defines a two-layer architecture — a portable, schema-versioned **Service Infrastructure Specification** (authored as data, per service) compiled by publishing into a sealed, environment-neutral **Infrastructure Contract** (the only artifact any downstream subsystem may consume) — plus a requirement/capability taxonomy that makes the model open-world and orchestrator-neutral. It preserves the existing pipeline, validator, and execution-context design, and it sequences the work into six sprints ending with contract authority (agent reads what publishing wrote), centralized tiered validation, and a portability proof.

---

## 2. Current Architecture Assessment (Deliverable 1)

### 2.1 What exists (verified)

**Model** (`Aovias.Deployment.Contracts`): `ServiceDeploymentBundle` = 13 per-concern contracts (manifest, deployment, filesystem, certificate, database, secret, configuration, dependency, health, network, volume, feature, runtime-capabilities) (`ContractModels.cs:3-32`). `InfrastructureContract` = aggregate view (filesystem, certificates, secrets, database, docker, network, storage, providers, dependencies, health) with `ContractVersion = "1.0"` (`Infrastructure/InfrastructureContract.cs:8-33`).

**Provisioning pipeline**: 9 ordered generic stages — Validation, Filesystem, Docker, Certificate, Secret, Storage, Database, Provider, Dependency (`InfrastructureProvisioningPipeline.cs:8-19`) — fail-fast, producing `InfrastructureReadinessReport` with blockers and `InfrastructureRecoveryCoordinator` recovery actions. Host actions abstracted behind `IInfrastructureExecutionContext` (docker engine inspect, network ensure, image exists, DB/message-bus reachability) — implemented by the Runtime Agent, `null` in offline validation (`IInfrastructureExecutionContext.cs:1-22`).

**Provider framework**: `ISecretProvider`/`ICertificateProvider` with `File*` implementations (`Infrastructure/Providers/InfrastructureProviderFramework.cs`) — the pluggable-backend seam anticipated by the gap analysis exists in embryo.

### 2.2 Producers / consumers / validators / runtime users

```
                         PRODUCERS
Publishing (ServiceContractGenerator)
  └─► ServiceContractBuilder ─► ServiceContractRegistry (hardcoded C# templates, 10 services + default)
        └─► ServiceDeploymentBundle
              └─► ServiceDeploymentBundleIO.WriteDirectory
                    ├─ 13 contract files (checksummed, in ContractFiles)
                    ├─ infrastructure.contract.json  ◄── InfrastructureContractBuilder (NOT in ContractFiles)
                    └─ identity.contract.json (+secrets) ◄── IdentityDeploymentContractBuilder (identity only)
LegacyContractCompatibilityBuilder ─► synthesized bundles for contract-less packages

                         CONSUMERS
Publishing:      ComposeGenerator/ComposeMountBuilder (Volume+Filesystem only), PackageVerifier
DRV:             ContractValidator (structural) + InfrastructureProvisioningValidationStage
                 (runs the SAME pipeline offline, Execution = null, host paths = validation config root)
Runtime Agent:   ReleaseDeploymentProvider — resolves bundle from package directory, then
                 REBUILDS the aggregate via InfrastructureContractBuilder.Build(bundle) and runs
                 the pipeline with a live Execution context (ReleaseDeploymentProvider.cs:426-443)
Planner:         DeploymentPlanner.Evaluate → blockers, startup order, required dirs/secrets/certs
CSM:             dashboards via ContractValidationReport / readiness reports

                         NON-CONSUMERS (should consume, do not)
VPS Bootstrap:   provision-deployment-storage.sh — hardcoded paths + REQUIRED_SUBDIRS list
                 synchronized with settings profiles by comment only
Runtime Agent release folders: created from ReleaseManagement/Docker settings, not contracts
                 (ReleaseDeploymentProvider.cs:403-419)
Monitoring/Certification/Rollback: consume health/deployment models, not the infrastructure contract
Documentation / Kubernetes generator: do not exist
```

### 2.3 Assessment verdict

The **pipeline architecture is sufficient**; the **contract architecture is not**. The single-source-of-truth property fails at both ends: at the source (infrastructure defined in code, in four parallel places) and at the sink (the unified artifact is never consumed; every consumer re-derives its own view).

---

## 3. Infrastructure Domain Model (Deliverable 2)

### 3.1 Coverage audit of the current model

| Domain | Current state | Verdict |
|---|---|---|
| Filesystem | `FilesystemContract` (dirs, perms, owner, uid/gid) | Present |
| Volumes | `VolumeContract` (HostToken → ContainerPath) | Present, Docker-shaped (token = env-var convention) |
| Networks | `NetworkContract` (`"${AOVIAS_DOCKER_NETWORK}"`, external flag) | Present, Docker-compose literal baked into the model |
| Certificates | `CertificateContract` (PFX-centric defaults, rotation flag) | Present, narrow (default `FileName = "identity-signing.pfx"` in the *generic* model) |
| Secrets | `SecretContract` (file source default) | Present; providers pluggable in embryo |
| Configuration | `ConfigurationContract` (required/optional keys, stringly `ValidationRule`) | Present |
| Storage | Conflated with Volumes (`InfrastructureContract.Storage = bundle.Volume`) | **Missing as a distinct domain** (no quotas, retention, backup) |
| Databases | `DatabaseContract` (single DB, provider/migration/seed/rollback strings) | Present; single-database assumption |
| Message brokers | Boolean flags (`RequiresRabbitMq`, `RequiresServiceBus`) + derived provider entries | **No first-class domain** |
| External providers | `ExternalProviderContract` (id, provider, secret, validation) | Present; instances hardcoded in builder |
| AI providers | Hardcoded `if (serviceId == "ai")` | **Not a model concept at all** |
| Health | `HealthContract` (liveness/readiness/startup/shutdown, probes) | Present, good |
| Ports | Inside `ServiceManifest.Docker.Ports` | **Not an infrastructure domain** (no protocol, exposure scope, ingress intent) |
| Environment variables | Nowhere in contracts — injected by compose builder code (M1 C2) | **Missing** |
| Dependencies | `DependencyContract` (service name, required, type="service") | Present; no version constraints, no readiness semantics, no external dependency kinds |
| Service discovery | Container-name convention only | **Missing** |
| Runtime requirements | `RuntimeCapabilities` (fixed boolean set) | Closed-world |
| Host / OS requirements | None (Ubuntu assumed) | **Missing** |
| Resource requirements | None (CPU/memory/disk) | **Missing** — required for K8s/ECS and enterprise capacity planning |
| Security requirements | uid/gid defaults only; no TLS policy, no capability drops, no user model | **Missing** |
| Observability | None (log paths implied by filesystem entries) | **Missing** |
| Recovery requirements | `RollbackPolicy` string on DatabaseContract only | **Fragmentary** |
| Lifecycle requirements | None (backup, upgrade, data migration, decommission) | **Missing** |
| Infrastructure capabilities | `RuntimeCapabilities` describes the *service's* needs, not what an *environment provides* | **Missing the environment side entirely** |

### 3.2 Target domain model

The model is reorganized into seven stable domain groups. Domains marked ★ are new.

```
InfrastructureContract v2
├── identity          service id, version, workload kind (http | worker | static | job), labels
├── resources
│   ├── compute ★     cpu/memory requests & limits, replicas policy
│   ├── filesystem    directories, permissions, ownership
│   ├── storage ★     volume claims (name, capacity, access mode, retention, backup policy)
│   ├── network       connectivity requirements (needs platform network, ingress intent)
│   └── ports ★       name, containerPort, protocol, exposure (internal | ingress | host)
├── security
│   ├── secrets       requirements + provider binding (file | vault | keyvault | offline-bundle)
│   ├── certificates  requirements + provider binding, rotation policy
│   └── runtime ★     run-as user/group, read-only rootfs, capability policy, TLS policy
├── data
│   ├── databases     list (not single), provider, migration/seed policy, connection secret ref
│   └── messaging ★   broker requirements (kind, connection secret ref, topology needs)
├── external          provider requirements[] (open taxonomy: AzureOpenAI, Sarvam, S3, SMTP, …)
├── operations
│   ├── health        liveness/readiness/startup probes (existing model, kept)
│   ├── observability ★  log streams, metrics endpoint, trace propagation intent
│   ├── lifecycle ★   startup class, shutdown grace, upgrade strategy, data-migration hooks
│   └── recovery ★    rollback safety (stateless | data-compatible | manual), backup preconditions
└── relationships
    ├── dependencies  service + external dependencies, version constraints, readiness semantics
    ├── discovery ★   published endpoints (name → port ref), consumed endpoints
    └── provides ★    capabilities THIS service offers the platform (e.g., RabbitMQ provides
                      capability "message-bus"), enabling infrastructure services as first-class citizens
```

Two cross-cutting constructs make the model open-world:

- **Requirement**: `{ kind, id, required, payload (typed per kind), providerBinding? }` — the extensible unit. New infrastructure concepts are new `kind` values with schemas, not new booleans.
- **Capability**: the environment-side mirror, declared during environment registration (`{ kind, id, properties }`). The Deployment Planner matches requirements against capabilities; a requirement with no matching capability is a deterministic blocker *before* deployment. This single abstraction is what later makes Kubernetes (capabilities implemented by the cluster), air-gapped installs (capability "offline-artifact-store"), and customer-managed infra (externally-provided capabilities) expressible without model changes.

`FeatureContract` and `RuntimeCapabilities` are both **absorbed** by requirements/capabilities and retired (compatibility projection during migration).

---

## 4. Contract Ownership Matrix (Deliverable 3)

Exactly one authoritative owner per verb. Everything else is a consumer.

| Concern | Owner | Explicitly NOT owned by | Current violations to fix |
|---|---|---|---|
| **Declaration** (what a service needs) | Service author, via per-service specification file in the service repo | `Aovias.Deployment.Contracts` code | `ServiceContractRegistry` switch; `InfrastructureContractBuilder` `ai` block; `IdentityDeploymentContractBuilder`; publishing's `GetDependencies` switch (M1 C1) |
| **Schema & semantics** (the language) | `Aovias.Deployment.Contracts` (models + JSON schema + `ContractValidator` core) | every other assembly | Feature/capability booleans; compose token in `NetworkContract` |
| **Compilation & sealing** (spec → contract in package) | Publishing (contract generation stage) | Runtime Agent, DRV | Agent rebuilding the aggregate at deploy time |
| **Structural validation** | Shared `ContractValidator` (single rule engine) | per-subsystem re-implementations | stringly `ValidationRule`; scattered ad-hoc checks |
| **Pre-deployment simulation** | DRV (`InfrastructureProvisioningValidationStage`, Execution = null) | Publishing (beyond structural), Agent | none — this is already correct |
| **Environment capability declaration** | Environment Registration (Runtime Agent `EnvironmentRegistrationStore`) | packages, publishing | capabilities currently implicit/absent |
| **Provisioning** (making host match contract) | Runtime Agent via `InfrastructureProvisioningPipeline` + `IInfrastructureExecutionContext` | Bootstrap scripts, CSM, deploy steps | bootstrap bash duplicating filesystem; agent's settings-driven release folders |
| **Installation** (pre-agent host prep) | Bootstrap — installs Docker + agent + config root **only**; consumes a generated host-preparation manifest derived from contracts, never its own path list | contract knowledge in bash | `provision-deployment-storage.sh` hardcoded paths/subdirs |
| **Execution** (deploy orchestration) | Deployment Orchestrator (workflow engine) — consumes contract-derived plan (`DeploymentPlanner`) | provisioning logic | acceptable today |
| **Verification/Certification** | Certification (post-deploy checks derived from health + requirements) | DRV re-runs | certification models not contract-fed yet |
| **Observation** | Monitoring — consumes `observability` domain | contract mutation | domain missing |
| **Documentation / future orchestrator generators** | Projection layer (compose today, K8s later) reading the sealed contract | hand-maintained infra docs | compose builder code-injected env vars |

---

## 5. Contract Hierarchy (Deliverable 4)

The proposed linear hierarchy (Infrastructure → Filesystem → Volumes → Providers → Dependencies) is **insufficient**: it is a containment list, not an architecture. It has no schema/versioning root, no separation between portable declaration and environment binding, and no place for orchestrator-specific output. The recommended hierarchy has four layers:

```
Layer 0 — SCHEMA           contract-schema vN (JSON Schema, versioned, published artifact;
                           migration policy: N reads N and N-1, publishing always writes N)
Layer 1 — SPECIFICATION    service.infrastructure.json — authored per service, in the service repo.
   (portable, declarative) Domain groups of §3.2. May reference shared platform profiles
                           ("aovias-http-service-defaults") instead of repeating boilerplate —
                           the successor of today's registry templates, as DATA shipped by the platform.
Layer 2 — CONTRACT         infrastructure.contract.json — compiled and sealed by publishing:
   (resolved, sealed)      profiles expanded, defaults applied, image digest bound, config keys merged,
                           schema-validated, checksummed. THE single consumed artifact.
                           Environment-neutral: contains requirements, never host values.
Layer 3 — BINDING          produced at deploy time per environment: requirements × environment
   (per environment)       capabilities + .env values → resolved mounts, network names, connection
                           targets. Owned by the target projection (docker-compose today; K8s manifests,
                           ECS task definitions later). Never stored in the package.
```

The 13 per-concern files remain as **projections of Layer 2 for compatibility** during migration and are retired once all consumers read the sealed contract (M2.3/M2.6). Compose ceases to be a package artifact generated from code-injected values and becomes a Layer 3 projection — this also resolves M1's N7 (baked profile paths) at the architectural root.

---

## 6. Infrastructure Lifecycle (Deliverable 5)

| Stage | Meaning | Owner | Artifact / record |
|---|---|---|---|
| 1. Declared | Service author writes/updates the specification | Service repo | `service.infrastructure.json` (Layer 1) |
| 2. Compiled | Profiles expanded, defaults resolved, schema-validated | Publishing (ContractGeneration stage) | sealed `infrastructure.contract.json` (Layer 2) |
| 3. Sealed | Checksummed into package, covered by deterministic fingerprint & certificate | Publishing (PackageGeneration/Certification) | `.aovdeploy` |
| 4. Validated | Structural + simulated provisioning, no live environment | DRV | `InfrastructureReadinessReport` (simulation) |
| 5. Admitted | Requirements matched against target environment capabilities; unmatched ⇒ deterministic blocker | Deployment Planner at environment registration / deployment-ready gate | admission report |
| 6. Provisioned | Host mutated to satisfy the contract (idempotent) | Runtime Agent (`InfrastructureProvisioningPipeline`) | `InfrastructureReadinessReport` (live) |
| 7. Verified | Post-provision assertions (paths writable, network exists, DB reachable, secrets resolvable) | Runtime Agent (same pipeline, verification mode) | stage results |
| 8. Operational | Containers started per binding; health green | Deployment Orchestrator | deployment record |
| 9. Certified | Contract-derived checklist verified against reality (every requirement demonstrably satisfied) | Certification | `PlatformCertificationChecklist` (extended to be contract-fed) |
| 10. Observed | Observability domain consumed; drift detection compares live state to contract | Monitoring / drift endpoint | drift report |
| 11. Recovered | Rollback honoring `recovery` domain (data-compatibility class gates automatic rollback) | Rollback workflow | rollback record |
| 12. Archived | Superseded contract versions retained with deployment history | Deployment History | history store |

Ownership rule: **stages 1–3 may only write; stages 4–12 may only read.** Any subsystem that needs infrastructure knowledge at stages 4–12 reads the sealed contract — never rebuilds, never consults settings for contract-covered concerns.

---

## 7. Gap Analysis (Deliverable 6)

| Sev | Gap | Evidence |
|---|---|---|
| **Critical** | Sealed contract has no consumers: excluded from `ContractFiles` (no checksum enforcement, `DeploymentPackageLayout.cs:34-49`), never read by `ReadDirectory`, rebuilt in memory by the agent (`ReleaseDeploymentProvider.cs:434`) and by DRV (`InfrastructureProvisioningValidationStage.cs:45`) | Single-source-of-truth property does not hold anywhere |
| **Critical** | Infrastructure declared in platform code: `ServiceContractRegistry` hardcoded switch for all 10 services (`ServiceContractRegistry.cs:30-44`); provider requirements for `ai` hardcoded in `InfrastructureContractBuilder.cs:74-95`; identity-specific builder | New services require modifying the contracts assembly — fails PRAMAN-independence and customer deployments |
| **Critical** | No environment capability model: nothing declares what a target environment provides; admission (lifecycle stage 5) cannot exist | `EnvironmentRegistrationStore` stores registration, not capabilities |
| **High** | Closed-world model: `FeatureContract`/`RuntimeCapabilities` fixed booleans (`ContractModels.cs:206-246`) | Every new infra concept is a breaking model change |
| **High** | Docker leakage into the portable layer: `NetworkContract.NetworkNameToken = "${AOVIAS_DOCKER_NETWORK}"` (`ContractModels.cs:187`), `VolumeMountContract.HostToken` env-var convention, `DockerInfrastructureContract` at contract root | Blocks orchestrator portability (Deliverable 7) |
| **High** | Parallel infrastructure definitions: bootstrap bash path list (`provision-deployment-storage.sh:19-40`), agent release folders from settings (`ReleaseDeploymentProvider.cs:403-419`), compose env vars from code (M1 C2), settings profiles | Four sources of filesystem truth |
| **High** | Missing domains: resources, ports-as-infrastructure, env vars, observability, lifecycle, recovery, security runtime, messaging, discovery (§3.1) | Confirmed against model source |
| **Medium** | Secret contracts incomplete (connection strings, RabbitMQ required-ness) — still open from gap analysis; `SqlSecrets()`/`ComplianceSecrets()` templates exist but P0 completion unverified end-to-end | `INFRASTRUCTURE_GAP_ANALYSIS.md` P0; registry templates |
| **Medium** | No schema artifact, no version negotiation: `ContractVersion` is the constant `"1.0"`; no JSON Schema; readers have no migration path | `ServiceContractBuilder.cs:8` |
| **Medium** | Dependency semantics too thin: no version constraints, no readiness gate semantics, single `type="service"` | `ContractModels.cs:135-147`; gap analysis "no readiness gates" |
| **Medium** | Validation rules stringly-typed (`ValidationRule = "non-empty"`) with interpretation scattered | `ContractModels.cs:132`, provider contract |
| **Low** | `LegacyContractCompatibilityBuilder` / `IsLegacySynthesized` retained after M1 makes contract-less packages impossible | `Building/LegacyContractCompatibilityBuilder.cs` |
| **Low** | Single-database assumption; certificate model defaults to identity PFX in generic model | `ContractModels.cs:56-99` |

---

## 8. Future Compatibility Review (Deliverable 7)

Test: can the target of §3–§5 support each environment **without redesign** (only new Layer 3 projections / capability adapters)?

| Target | With current model | With proposed model | Required abstraction (all already in §3) |
|---|---|---|---|
| Docker Compose (today) | ✅ (it *is* the model) | ✅ Layer 3 projection | — |
| Docker Swarm | ⚠️ partial | ✅ | compute/replicas domain; projection |
| Kubernetes | ❌ HostToken mounts, compose network token, no resources/ports domains | ✅ | volume claims → PVCs; ports → Services; health → probes; secrets provider binding → K8s Secrets/CSI; capability adapter per cluster |
| Azure Container Apps / AWS ECS | ❌ same reasons | ✅ | same; provider bindings map to cloud secret stores |
| Nomad | ❌ | ✅ | projection only |
| Air-gapped | ⚠️ images in package (good), but provider reachability checks assume connectivity | ✅ | capability `offline-artifact-store`; requirements marked `deferrable`; provider binding `offline-bundle` |
| Hybrid / customer-managed infra | ❌ platform provisions everything or nothing | ✅ | capability declared as *externally provided* — planner admits without provisioning, verifies reachability only |
| Offline installation | ⚠️ | ✅ | bootstrap consumes generated host-preparation manifest (no network fetch) |
| Multi-framework services | ⚠️ .NET/Angular/Static via providers; contract assumes HTTP+wget healthcheck | ✅ | workload kinds + probe kinds (http/tcp/exec) |

Conclusion: portability requires **no redesign of the pipeline**, but it does require the Layer 2 contract to be scrubbed of Docker literals and enriched with the ★ domains — that is exactly the M2 scope. Deferring it means every one of these targets later forces a breaking contract migration across all published packages.

---

## 9. Extensibility Review (Deliverable 8)

Can a completely new service describe itself without architectural modification? Today: **no** (requires editing `ServiceContractRegistry` + possibly `FeatureContract`). Under this specification: **yes** — a data file per service. Two probes:

**Redis (infrastructure service — provides a capability):**
```json
{ "schema": "aovias/infrastructure-spec@2",
  "identity": { "serviceId": "redis", "workload": "stateful" },
  "resources": {
    "compute": { "memory": { "request": "256Mi", "limit": "1Gi" } },
    "storage": [ { "name": "redis-data", "capacity": "5Gi", "retention": "persistent", "backup": "daily" } ],
    "ports": [ { "name": "redis", "containerPort": 6379, "protocol": "tcp", "exposure": "internal" } ] },
  "security": { "secrets": [ { "id": "redis-password", "required": true, "provider": "file" } ] },
  "operations": {
    "health": { "probe": { "kind": "exec", "command": ["redis-cli", "ping"] } },
    "recovery": { "rollbackSafety": "data-compatible" } },
  "relationships": { "provides": [ { "kind": "cache", "id": "redis" } ] } }
```
Non-HTTP health probe, persistent storage with retention, `provides` — all expressible; consumers (`ai`, sessions) then simply declare `requires: { kind: "cache" }` and the planner binds them. None of this fits today's model (no exec probes, no storage domain, no provides).

**Prometheus (observability consumer + scraper):** declares a `metrics-scrape` capability provided; every other service's `observability.metricsEndpoint` becomes discoverable data instead of convention. RabbitMQ, ElasticSearch, MinIO, Grafana, and vector databases follow the same pattern — each was checked against the domain model and requires **zero new model concepts**, only new requirement/capability `kind` values (registered in the extensible taxonomy, not compiled into booleans).

The current model fails all eight examples on at least one axis (probe kind, storage semantics, provides, ports, or resource limits).

---

## 10. Validation Architecture (Deliverable 9)

Principle: **one rule engine, five tiers, zero duplicated rules.** Each tier adds information the previous tier could not have; no tier re-implements a lower tier.

| Tier | Question | Executor | Exists today? |
|---|---|---|---|
| T0 Schema | Is the document well-formed against `contract-schema vN`? | JSON Schema validation — publishing, and any consumer on read | ❌ (no schema) |
| T1 Semantic | Are cross-field invariants satisfied (volume↔filesystem consistency, health coherence, dependency acyclicity, secret references resolvable-in-principle)? | Shared `ContractValidator` core — **the existing one, extended**; invoked by publishing, DRV, agent | ✅ partial (`ContractValidator.Validate`, `ComposeMountValidator` as projection check) |
| T2 Simulation | Would provisioning succeed, environment-independent? | Existing pipeline with `Execution = null` — DRV `InfrastructureProvisioningValidationStage` | ✅ already correct |
| T3 Environment | Does THIS environment satisfy requirements (capability match, docker engine, DB/broker reachability, write access)? | Existing pipeline with live `IInfrastructureExecutionContext` — Runtime Agent; capability admission added at registration/planner | ✅ pipeline / ❌ admission |
| T4 Verification | Post-provision & post-deploy: is reality equal to contract (drift)? | Certification + drift endpoint reading the same contract | ⚠️ certification exists, not contract-fed |

Anti-duplication rules: DRV and the agent already share T2/T3 via the same pipeline — preserve this. Publishing must not re-implement T3 (its DRV stage invokes the shared stack). `ComposeMountValidator` is reclassified as a **projection validator** (Layer 3 output check), not a contract rule. Stringly `ValidationRule` values are replaced by typed rule identifiers owned by the T1 engine. Every tier emits the same `InfrastructureStageResult`/blocker shape so CSM renders one readiness language everywhere.

---

## 11. Technical Debt Report (Deliverable 11)

| # | Item | Evidence | Disposition |
|---|---|---|---|
| T1 | `ServiceContractRegistry` — 507 lines of hardcoded per-service templates | `Building/ServiceContractRegistry.cs:30-44` | **Refactor → data**: templates become shipped platform profile *files* (Layer 1 defaults); registry class reduces to profile loader, then **Delete** |
| T2 | Service knowledge in `InfrastructureContractBuilder.BuildProviders` (`ai`, RabbitMQ/ServiceBus flag translation) | `Infrastructure/InfrastructureContractBuilder.cs:44-109` | **Delete** logic; providers come from the spec's `external` domain |
| T3 | `IdentityDeploymentContractBuilder` + identity-only package files | `Building/IdentityDeploymentContractBuilder.cs` | **Merge**: identity's needs (signing cert, secrets) are expressible in the generic security domain; keep files temporarily as compatibility projection |
| T4 | `LegacyContractCompatibilityBuilder` / `IsLegacySynthesized` | `Building/LegacyContractCompatibilityBuilder.cs` | **Delete** after M1's "no contract-less packages" gate ships; agent rejects instead of synthesizing |
| T5 | `FeatureContract` + `RuntimeCapabilities` closed boolean sets | `ContractModels.cs:206-246` | **Refactor**: absorbed by requirements/capabilities; retained one schema version as derived projection, then **Delete** |
| T6 | Decorative `infrastructure.contract.json` (written, unchecksummed, unread) | `ServiceDeploymentBundleIO.cs:57-60`; `DeploymentPackageLayout.cs:34-49` | **Refactor**: becomes the authoritative sealed artifact (checksummed, fingerprinted, read by all consumers) |
| T7 | Dual aggregate construction (publish-time write + deploy-time/DRV rebuild) | `ReleaseDeploymentProvider.cs:434`; `InfrastructureProvisioningValidationStage.cs:45` | **Merge**: single read path via `DeploymentPackageContractsResolver`; rebuild path deleted |
| T8 | Bootstrap bash duplicating filesystem contract (paths + subdir list synced by comment) | `aovias-config/scripts/provision-deployment-storage.sh:19-45` | **Refactor**: bootstrap consumes a generated host-preparation manifest; script becomes a thin executor |
| T9 | Agent release-management folders provisioned from settings outside any contract | `ReleaseDeploymentProvider.cs:403-419` | **Refactor**: agent's own infrastructure declared as the runtime-agent service's contract (it is a service; it should eat the same food) |
| T10 | Docker literals in portable model (`NetworkNameToken`, `HostToken`, `DockerInfrastructureContract` at root) | `ContractModels.cs:185-204`; `InfrastructureContract.cs:22,35-48` | **Refactor**: move to Layer 3 binding; core model keeps orchestrator-neutral claims |
| T11 | Two provisioner families: `Provisioning/Contract*Provisioner` (older) wrapped by `Infrastructure/Provisioners/*` | `Provisioning/` vs `Infrastructure/Provisioners/` (e.g., Filesystem provisioner delegates to `ContractFilesystemProvisioner`) | **Merge**: keep the pipeline stages as the only entry; fold `Provisioning/` helpers into them as internals |
| T12 | Duplicated `HostPathContext` derivations (DRV builds one from config root, agent resolves another) | `InfrastructureProvisioningValidationStage.cs:35-41` | **Refactor**: host-path resolution becomes part of Layer 3 binding, single resolver |
| T13 | Stringly-typed policies: `Migration`/`Seed`/`StartupPolicy`/`RollbackPolicy`/`ValidationRule` free strings | `ContractModels.cs:82-133` | **Refactor** to schema-constrained enums in v2 |
| T14 | `ServiceContractRegistry.cs` encoding anomaly (tools detect it as binary; UTF-16/BOM artifacts) | grep "binary file matches" | **Fix** opportunistically with T1 |

---

## 12. Engineering Roadmap (Deliverable 10)

Sprints are sequenced so every sprint leaves the platform deployable and each unlocks the next. Complexity: S/M/L.

**M2.1 — Contract Schema v2 & the Sealed Artifact (L)**
*Objective*: define `contract-schema v2` (domains of §3.2, requirement/capability constructs, typed policies) as a published JSON Schema; make `infrastructure.contract.json` checksummed, fingerprinted, and schema-validated at publish.
*Dependencies*: M1 (deterministic packaging, additive layout rule).
*Scope*: schema artifact + versioning/migration policy (writer at N, readers N/N-1); add file to `ContractFiles`; T0 validation in publishing and on every read; v1→v2 projection for the 13 legacy files. Out: consumer switchover.
*DoD*: every published package carries a schema-valid, checksummed v2 contract; v1 files still emitted.
*Regression*: schema round-trip; checksum enforcement; fingerprint covers the contract; v1 projection equivalence.
*Acceptance*: 10/10 services publish with v2 contracts; DRV/agent unaffected.
*Risk*: schema churn — mitigate with review gate on v2 before sprint exit. 

**M2.2 — Declaration as Data (L)**
*Objective*: eliminate infrastructure-in-code.
*Dependencies*: M2.1.
*Scope*: per-service `service.infrastructure.json` + shipped platform profiles replacing registry templates; publishing compiles spec→contract; delete T1/T2/T3 hardcoding and publishing's dependency switch (M1 C1 closes here); grep gates.
*DoD*: adding a fixture service requires **zero** platform-code changes; 10/10 specs authored and byte-equivalent (post-compilation) to previous contracts.
*Regression*: golden-fingerprint equality pre/post migration per service; new-service fixture test.
*Acceptance*: contract diff across migration = empty for all services.
*Risk*: silent semantic drift during template→data transcription — mitigated by the equality gate.

**M2.3 — Contract Authority (M)**
*Objective*: one write path, one read path.
*Dependencies*: M2.1.
*Scope*: agent and DRV read the sealed contract via `DeploymentPackageContractsResolver`; delete rebuild paths (T7); publish↔runtime contract-equality assertion in DRV; agent's own folders contract-declared (T9); retire `LegacyContractCompatibilityBuilder` (T4).
*DoD*: `InfrastructureContractBuilder.Build` has zero production callers at deploy/validation time.
*Regression*: tamper test (modified contract in package → checksum failure → deterministic block); equality assertion test.
*Acceptance*: full-platform deploy on test VPS driven exclusively by sealed contracts.
*Risk*: hidden dependencies on rebuild-time defaults — surfaced by the equality assertion in M2.1's projection.

**M2.4 — Capabilities & Admission (M)**
*Objective*: environment side of the model.
*Dependencies*: M2.2, M2.3.
*Scope*: capability declaration in environment registration; `DeploymentPlanner` admission (requirements × capabilities) as the Deployment-Ready gate input; externally-provided capability class (customer-managed infra); air-gapped/deferrable requirement semantics.
*DoD*: deploying a service requiring an undeclared capability is blocked at admission with recovery guidance, before any provisioning.
*Regression*: admission matrix tests (satisfied / missing / externally-provided / deferrable).
*Acceptance*: CSM shows capability match report per environment.
*Risk*: capability taxonomy sprawl — governed kind registry with review.

**M2.5 — Validation Consolidation (M)**
*Objective*: the five-tier architecture of §10 with zero duplicated rules.
*Dependencies*: M2.1–M2.3.
*Scope*: typed rule identifiers replacing `ValidationRule` strings (T13); `ComposeMountValidator` reclassified as projection validator; single blocker/report shape across tiers; provisioner family merge (T11) and host-path resolver unification (T12).
*DoD*: every validation rule exists in exactly one place; tier provenance visible in reports.
*Regression*: rule-coverage inventory test; per-tier failure-injection suite.
*Acceptance*: DRV, agent, and publishing reports render from one shared model in CSM.
*Risk*: behavioral changes from rule unification — snapshot current pass/fail matrix first.

**M2.6 — Portability Proof (S/M)**
*Objective*: prove Layer 3 with a second projection.
*Dependencies*: M2.1–M2.5.
*Scope*: extract `IDeploymentTargetBinding`; compose generator moves behind it (resolving M1 N7 at the root — compose becomes environment-bound output, not a package artifact, with package-format compatibility shim); spike Kubernetes manifest projection for 2 services (fixture-grade, not production).
*DoD*: compose output byte-identical through the binding seam; K8s spike renders valid manifests from unmodified sealed contracts.
*Regression*: compose-equivalence golden test.
*Acceptance*: architecture review signs off that no Docker literal remains in Layer 2.
*Risk*: package-format expectations on compose file — retain compose emission behind the compatibility shim until M3+ consumers migrate.

---

## 13. Definition of Done (Deliverable 12)

Milestone M2 is complete when all of the following are true and mechanically verified:

- The sealed `infrastructure.contract.json` (schema v2) fully describes every infrastructure requirement of every deployable service; it is checksummed, fingerprint-covered, and schema-validated.
- Every infrastructure dependency originates from per-service specification data; `Aovias.Deployment.Contracts`, publishing, bootstrap scripting, and the agent contain **zero** service-specific infrastructure knowledge (grep-gated: no service-id switches, no `ai` blocks, no path lists).
- Publishing, DRV, Runtime Agent, Deployment Planner, and Certification all consume the same sealed contract through one read path; the deploy-time rebuild path no longer exists.
- Bootstrap consumes a contract-derived host-preparation manifest; the bash path list is gone.
- No duplicated infrastructure definitions remain (contract vs settings vs scripts vs compose-builder code) — settings retain only agent-operational concerns, and the agent's own layout is contract-declared.
- Validation is centralized in the five-tier architecture; every rule has exactly one implementation and reports through one shape.
- Environment capabilities are declared at registration and admission blocks unsatisfiable deployments deterministically, including externally-provided and air-gapped capability classes.
- Layer 2 contains no orchestrator-specific constructs; the compose projection runs behind the target-binding seam and a second (K8s) projection has been demonstrated from unmodified contracts.
- A brand-new service (fixture) can be added to the platform — declared, published, validated, admitted, provisioned — with zero platform-code changes.
- Contract schema versioning and migration policy are published; v1→v2 migration executed for all 10 services with fingerprint-equality evidence.

---

## Appendix — Engineering-principles compliance

Platform-first: the requirement/capability model and Layer 1–3 separation are designed for the 10-year horizon (K8s, multi-cloud, air-gapped, customer-managed), not for the current compose workflow — which becomes just the first binding. Stable abstractions: the pipeline, execution context, and validator — the parts that have proven themselves — are retained; only the closed-world model and code-as-declaration are replaced. Single ownership: §4 assigns every verb exactly one owner and §6 forbids reads from writing. No parallel implementations: every sprint deletes its predecessor (rebuild path, registry, legacy builder, bash lists) rather than layering beside it. Every finding above was verified against the current tree; file/line citations are included throughout.

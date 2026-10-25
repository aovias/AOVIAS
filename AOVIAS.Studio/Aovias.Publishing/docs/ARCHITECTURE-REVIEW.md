# AOVIAS Publishing Engine - Architecture Review (Sprint 8)

This document captures the production-readiness architecture review of the AOVIAS
Publishing Engine and the engineering-standards baseline established in Sprint 8.

## Components

| Project | Responsibility |
|---------|----------------|
| `Aovias.Publishing.Contracts` | Models, interfaces, package layout constants |
| `Aovias.Publishing.Core` | Pipeline, generators, packaging, CLI execution, shared helpers |
| `Aovias.Publishing.Providers.{DotNet,Angular,Static}` | Technology-specific discovery, build, publish |
| `Aovias.Publishing.SDK` | DI composition + `PublishingClient` facade |
| `Aovias.DeploymentValidation` | Deployment Readiness Validation (DRV) stages + simulation |
| `Aovias.Configuration` | Profiles, `DeploymentProfile`, path resolution, settings |

## Publishing pipeline (high level)

Discover -> Validate -> Resolve Dependencies -> Build -> Publish -> Inspect ->
Check Docker -> Docker Build -> Docker Export -> Generate (manifest, compose,
health, config-template, env, metadata) -> Stage -> Interim package + checksums
-> Package Verification -> Deployment Readiness (DRV: simulation, bootstrap,
health, API) -> Quality Gates -> Publishing Report -> Finalize -> Release Library.

## Deployment Readiness Validation (DRV) stages

Package, Compatibility, Docker Compose, Bootstrap, Configuration, Runtime
(container startup), Health, API, Rollback, Environment Neutral. Stages that
require Docker run inside an isolated temporary workspace that extracts the
package, creates a dedicated network, `docker load`s the image, `compose up`s
the stack, probes endpoints, then tears everything down on dispose.

## Engineering-standards baseline (Sprint 8, Phase 0)

The following debt was removed or consolidated:

- `CopyDirectory` was duplicated across the DotNet/Angular/Static providers.
  Consolidated into `Aovias.Publishing.Core.Common.PublishingFileSystem`
  (relative-path composition, no fragile string replacement).
- `ToPlaceholder` was duplicated in `ConfigTemplateGenerator` and
  `DeploymentArtifactSanitizer`. Consolidated into
  `Aovias.Publishing.Core.Common.PlaceholderFormatter`.
- Runtime Agent detection was scattered across the discovery provider,
  sanitizer, compose generator and Docker context stager. Consolidated into
  `Aovias.Publishing.Core.Common.RuntimeAgentConventions` (service id, default
  port, entry point, detection).
- `dotnet publish` no longer string-matches `ComplianceService.csproj`. The
  decision to exclude referenced appsettings is now discovery metadata
  (`ProjectDiscoveryResult.PublishProperties`) produced by the DotNet provider;
  the CLI executor is generic and simply forwards `/p:` properties.
- Hardcoded `Publisher = "AOVIAS"` moved to `PublishingSettings.PublisherName`.
- Hardcoded `RuntimeCompatibility = "linux/net10.0"` is now derived from
  `PublishingSettings.TargetRuntimeOs` + the discovered framework.
- Hardcoded port fallback `7080` replaced with `RuntimeAgentConventions.DefaultPort`.

## Conventions going forward

- No string concatenation for filesystem paths; use `Path.Combine` /
  `Path.GetRelativePath`.
- No hardcoded environment-specific (Windows or Linux) paths in code; resolve
  from the active `DeploymentProfile`.
- Every generated artifact must be environment-neutral and pass the DRV gates.
- Every fixed bug becomes a permanent regression test.

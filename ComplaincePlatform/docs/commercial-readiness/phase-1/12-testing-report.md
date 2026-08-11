# Phase 1 — Testing Report

**Sprint:** 6  
**Status:** Focused unit tests added

## Added / extended

| Project | Test | Coverage |
|---------|------|----------|
| `OrganizationService.Tests` | `OrganizationalUnitHierarchyPolicyTests` | Allowed / rejected OU edges, known types |
| `ComplianceService.Tests` | `FrameworkLifecycleServiceTests` | Single-active-version on activate; archived immutability |
| `ComplianceService.Tests` | `EvidenceVerificationStateMachineTests` | Double-approve / terminal Verified + happy-path smoke |

## Not added (honest gaps)

| Item | Reason |
|------|--------|
| `EvidenceExpiryJob` Hangfire smoke | Job does not exist yet (journey `ComplianceExpiryJob` only) |
| End-to-end CommunicationService delivery | Publisher is console/ILogger in Phase 1 |
| Gateway `ComplianceSearchController` integration test | Thin facade over existing graph search; covered by contract docs |

## How to run

```bash
dotnet test OrganizationService.Tests/OrganizationService.Tests.csproj --filter OrganizationalUnitHierarchyPolicyTests
dotnet test ComplianceService.Tests/ComplianceService.Tests.csproj --filter "FrameworkLifecycleServiceTests|EvidenceVerificationStateMachineTests"
```

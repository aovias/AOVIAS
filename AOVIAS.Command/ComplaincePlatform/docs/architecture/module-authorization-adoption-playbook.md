# Module Authorization Adoption Playbook (Phase 5)

## Goal

Migrate API endpoints from legacy `[Authorize(Roles=...)]` to runtime `[RequirePermission(...)]` via `Shared.Authorization`.

## Steps per endpoint

1. Add `Shared.Authorization` project reference to the service `.csproj`.
2. Register `AddPramanAuthorization(configuration)` after `AddAuthorization()` in `Program.cs`.
3. Ensure `InternalApiKeyForwardingHandler` is registered for service-to-service calls.
4. Define permission code constants (strings only) in `{Module}PermissionCodes.cs`.
5. Replace role checks with `[RequirePermission(ModulePermissionCodes.Action)]` on controller actions.
6. Keep module scope JWT policy on controller class (e.g. `ComplianceScope`) as authentication gate.
7. Seed permissions in AccessGovernance and assign to enterprise roles.
8. Add authorization evaluation test with mocked or in-memory AccessGovernance.

## Pilot (ComplianceService)

- `POST /api/v1.1/evidence/objective-elements/{objectiveElementId}/verify/{evidenceId}` uses `Compliance.Evidence.Verify`.

## Context for RLS

Pass resource scope via route values or explicit evaluate call:

```csharp
Context = new Dictionary<string, string>
{
    ["resource_department_id"] = departmentId.ToString()
}
```

## Do not

- Embed permissions in JWT
- Implement module-local permission engines
- Duplicate AccessGovernance aggregates

using System.Security.Claims;
using EnterpriseDirectoryService.Application.Contracts;
using Shared.Authorization;

namespace EnterpriseDirectoryService.Application.Directory.Security;

public interface IDirectoryAuthorizationService
{
    Task<GovernedDirectoryContext> BuildContextAsync(ClaimsPrincipal user, Guid? organizationId, CancellationToken ct = default);
    Task EnsureSearchAllowedAsync(ClaimsPrincipal user, Guid? organizationId, CancellationToken ct = default);
    Task EnsureProfileAllowedAsync(ClaimsPrincipal user, Guid? organizationId, CancellationToken ct = default);
}

public sealed class DirectoryAuthorizationService(IAuthorizationEvaluator evaluator) : IDirectoryAuthorizationService
{
    public async Task<GovernedDirectoryContext> BuildContextAsync(ClaimsPrincipal user, Guid? organizationId, CancellationToken ct = default)
    {
        var searchReq = AuthorizationContextBuilder.FromPermissionCode(user, "Directory.Search.Read");
        if (organizationId.HasValue)
            searchReq = CloneWithOrg(searchReq, organizationId);

        var searchDecision = await evaluator.EvaluateAsync(searchReq, ct);
        var contactDecision = await evaluator.EvaluateAsync(
            CloneWithOrg(AuthorizationContextBuilder.FromPermissionCode(user, "Directory.Contact.Read"), organizationId), ct);
        var credentialDecision = await evaluator.EvaluateAsync(
            CloneWithOrg(AuthorizationContextBuilder.FromPermissionCode(user, "Directory.Credential.Read"), organizationId), ct);

        var rls = searchDecision.RowLevelFilter;
        return new GovernedDirectoryContext
        {
            SearchAllowed = searchDecision.Allowed,
            ContactReadable = contactDecision.Allowed,
            CredentialReadable = credentialDecision.Allowed,
            RowLevelFilter = new RowLevelFilterScope
            {
                OrganizationId = rls?.OrganizationId ?? organizationId,
                HospitalId = rls?.HospitalId,
                DepartmentId = rls?.DepartmentId
            }
        };
    }

    public async Task EnsureSearchAllowedAsync(ClaimsPrincipal user, Guid? organizationId, CancellationToken ct = default)
    {
        var ctx = await BuildContextAsync(user, organizationId, ct);
        if (!ctx.SearchAllowed)
            throw new UnauthorizedAccessException("Directory.Search.Read permission required.");
    }

    public async Task EnsureProfileAllowedAsync(ClaimsPrincipal user, Guid? organizationId, CancellationToken ct = default)
    {
        var req = AuthorizationContextBuilder.FromPermissionCode(user, "Directory.Profile.Read");
        if (organizationId.HasValue) req = CloneWithOrg(req, organizationId);
        var decision = await evaluator.EvaluateAsync(req, ct);
        if (!decision.Allowed)
            throw new UnauthorizedAccessException("Directory.Profile.Read permission required.");
    }

    private static AuthorizationRequest CloneWithOrg(AuthorizationRequest req, Guid? organizationId) => new()
    {
        SubjectId = req.SubjectId,
        PersonId = req.PersonId,
        EmploymentId = req.EmploymentId,
        ProfessionalProfileId = req.ProfessionalProfileId,
        OrganizationId = organizationId ?? req.OrganizationId,
        HospitalId = req.HospitalId,
        DepartmentId = req.DepartmentId,
        Resource = req.Resource,
        Action = req.Action,
        Context = req.Context
    };
}

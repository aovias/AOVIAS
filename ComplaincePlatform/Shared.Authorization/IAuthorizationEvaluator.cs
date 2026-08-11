namespace Shared.Authorization;

public interface IAuthorizationEvaluator
{
    Task<AuthorizationDecision> EvaluateAsync(AuthorizationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EffectivePermission>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

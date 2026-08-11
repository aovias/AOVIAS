using IdentityService.Application.DTOs;

namespace IdentityService.Application.Contracts;

public interface IPersonServiceClient
{
    Task<PersonLinkResult?> CreatePersonForRegistrationAsync(string email, string? displayName, CancellationToken cancellationToken = default);
}

public sealed class PersonLinkResult
{
    public Guid PersonId { get; init; }
    public string PlatformId { get; init; } = string.Empty;
}

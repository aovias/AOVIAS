namespace IdentityService.Application.Contracts;

public interface IWorkforceContextClient
{
    Task<Guid?> GetPrimaryEmploymentIdAsync(Guid personId, CancellationToken cancellationToken = default);
}

public interface IProfessionalContextClient
{
    Task<Guid?> GetProfessionalProfileIdAsync(Guid personId, CancellationToken cancellationToken = default);
}

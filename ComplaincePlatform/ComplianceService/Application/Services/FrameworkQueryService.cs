using ComplianceService.Application.Contracts;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Application.Services;

/// <summary>
/// Service for framework query operations.
/// Implements CQRS read side - orchestrates business logic and delegates data access to repositories.
/// </summary>
public class FrameworkQueryService : IFrameworkQueryService
{
    private readonly IFrameworkRepository _frameworkRepository;
    private readonly ILogger<FrameworkQueryService> _logger;

    public FrameworkQueryService(
        IFrameworkRepository frameworkRepository,
        ILogger<FrameworkQueryService> logger)
    {
        _frameworkRepository = frameworkRepository;
        _logger = logger;
    }

    public async Task<List<FrameworkDto>> GetAllFrameworksAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service: Retrieving all active frameworks");

        var frameworks = await _frameworkRepository.GetAllActiveFrameworksAsync(cancellationToken);

        _logger.LogInformation("Service: Retrieved {Count} active frameworks", frameworks.Count);

        return frameworks;
    }

    public async Task<List<HospitalFrameworkDto>> GetHospitalFrameworksAsync(Guid hospitalId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service: Retrieving frameworks for hospital {HospitalId}", hospitalId);

        var frameworks = await _frameworkRepository.GetHospitalFrameworksAsync(hospitalId, cancellationToken);

        _logger.LogInformation("Service: Retrieved {Count} frameworks for hospital {HospitalId}", 
            frameworks.Count, hospitalId);

        return frameworks;
    }
}

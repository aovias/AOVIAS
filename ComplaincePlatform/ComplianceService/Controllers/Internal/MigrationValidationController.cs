using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.Internal;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/internal/migration-validation")]
[Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
public sealed class MigrationValidationController : ControllerBase
{
    private readonly IDataMigrationValidationService _validationService;

    public MigrationValidationController(IDataMigrationValidationService validationService)
    {
        _validationService = validationService;
    }

    [HttpGet("report")]
    public async Task<ActionResult<ApiResponse<DataMigrationValidationReportDto>>> GetReport(CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        var report = await _validationService.ValidateAsync(cancellationToken);
        var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, report, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, response);
    }
}

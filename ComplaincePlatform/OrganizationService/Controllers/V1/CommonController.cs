using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrganizationService.Application.DTOs;
using OrganizationService.Application.Services;
using Shared.Infrastructure.Api;

namespace OrganizationService.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/common")]
    [Authorize(Policy = "OrganizationScope")]
    public class CommonController : ControllerBase
    {
        private readonly CommonLookupAppService _commonLookupAppService;

        public CommonController(CommonLookupAppService commonLookupAppService)
        {
            _commonLookupAppService = commonLookupAppService;
        }

        [HttpGet("departments")]
        [ProducesResponseType(typeof(IReadOnlyList<DepartmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
        {
            var traceId = HttpContext.TraceIdentifier;

            var result = await _commonLookupAppService.GetActiveDepartmentsAsync(cancellationToken);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (IReadOnlyList<DepartmentDto>)result,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }

        [HttpGet("services")]
        [ProducesResponseType(typeof(IReadOnlyList<ServiceDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetServices([FromQuery] Guid? departmentId, CancellationToken cancellationToken)
        {
            var traceId = HttpContext.TraceIdentifier;

            var result = await _commonLookupAppService.GetActiveServicesAsync(departmentId, cancellationToken);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (IReadOnlyList<ServiceDto>)result,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }

        [HttpGet("master-departments")]
        [ProducesResponseType(typeof(IReadOnlyList<MasterDepartmentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMasterDepartments(CancellationToken cancellationToken)
        {
            var traceId = HttpContext.TraceIdentifier;

            var result = await _commonLookupAppService.GetMasterDepartmentsAsync(cancellationToken);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (IReadOnlyList<MasterDepartmentDto>)result,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
    }
}

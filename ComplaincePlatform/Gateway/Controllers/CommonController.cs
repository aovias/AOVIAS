using Gateway.Application.Queries;
using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace Gateway.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/common")]
    [Authorize(Policy = AuthConstants.Policies.ComplianceScope)]
    public class CommonController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CommonController> _logger;

        public CommonController(
            IMediator mediator,
            ILogger<CommonController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("departments")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DepartmentDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> GetDepartments(
            CancellationToken cancellationToken)
        {
            var traceId = HttpContext.TraceIdentifier;

            _logger.LogInformation(
                "GET /api/v1/common/departments - Dispatching GetDepartmentsQuery, TraceId: {TraceId}",
                traceId);

            var query = new GetDepartmentsQuery(traceId);
            var response = await _mediator.Send(query, cancellationToken);

            return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
        }

        [HttpGet("services")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ServiceDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ServiceDto>>>> GetServices(
            [FromQuery] Guid? departmentId,
            CancellationToken cancellationToken)
        {
            var traceId = HttpContext.TraceIdentifier;

            _logger.LogInformation(
                "GET /api/v1/common/services - Dispatching GetServicesQuery, TraceId: {TraceId}",
                traceId);

            var query = new GetServicesQuery(departmentId, traceId);
            var response = await _mediator.Send(query, cancellationToken);

            return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
        }

        [HttpGet("master-departments")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MasterDepartmentDto>>), StatusCodes.Status200OK)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<MasterDepartmentDto>>>> GetMasterDepartments(
            CancellationToken cancellationToken)
        {
            var traceId = HttpContext.TraceIdentifier;

            _logger.LogInformation(
                "GET /api/v1/common/master-departments - Dispatching GetMasterDepartmentsQuery, TraceId: {TraceId}",
                traceId);

            var query = new GetMasterDepartmentsQuery(traceId);
            var response = await _mediator.Send(query, cancellationToken);

            return StatusCode(response.Error?.Status ?? StatusCodes.Status200OK, response);
        }
    }
}

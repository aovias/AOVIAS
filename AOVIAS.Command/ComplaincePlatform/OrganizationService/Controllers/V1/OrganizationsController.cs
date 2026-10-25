using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Services;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;

namespace OrganizationService.Controllers.V1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/organizations")]
    [Authorize(Policy = "OrganizationScope")]
    public class OrganizationsController : ControllerBase
    {
        private readonly OrganizationAppService _orgService;
        public OrganizationsController(OrganizationAppService organizationAppService)
        {
            this._orgService = organizationAppService;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            var traceId = HttpContext.TraceIdentifier;

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)"Organization API v1 is running",
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(CreateOrganizationDto dto)
        {
            var traceId = HttpContext.TraceIdentifier;

            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            var org = await _orgService.CreateAsync(dto, userId);

            var response = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                (object)org,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }

        [HttpGet("{id}/detail")]
        [ProducesResponseType(typeof(OrganizationService.Application.DTOs.OrganizationDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrganizationDetail(Guid id, CancellationToken cancellationToken)
        {
            var traceId = HttpContext.TraceIdentifier;

            var detail = await _orgService.GetOrganizationDetailAsync(id, cancellationToken);
            if (detail == null)
            {
                var failure = ApiResponseFactory.Failure<OrganizationService.Application.DTOs.OrganizationDetailResponseDto>(
                    ApiResponseCode.RESOURCE_NOT_FOUND,
                    traceId);

                var metaNotFound = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(metaNotFound.HttpStatus, failure);
            }

            var success = ApiResponseFactory.Success(
                ApiResponseCode.SUCCESS,
                detail,
                traceId);

            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, success);
        }

        //[HttpGet("Organizations")]
        //public async Task<IActionResult> MyOrganizations()
        //{
        //    var userId = Guid.Parse(User.FindFirst("sub")!.Value);

        //    var orgs = await _orgService.

        //    return Ok(orgs);
        //}

        //[HttpPost("AddMember")]
        //public async Task<IActionResult> AddMember(Guid orgId, AddMemberDto dto)
        //{
        //    _db.Members.Add(new OrganizationMember
        //    {
        //        Id = Guid.NewGuid(),
        //        OrganizationId = orgId,
        //        UserId = dto.UserId,
        //        Role = dto.Role
        //    });

        //    await _db.SaveChangesAsync();
        //    return Ok();
        //}

        //[HttpPut("Update")]
        //public async Task<IActionResult> Update(Guid id, UpdateOrganizationDto dto)
        //{
        //    var org = await _db.Organizations.FindAsync(id);

        //    org.Name = dto.Name;
        //    org.Address = dto.Address;
        //    _db.Entry(org).Property(x => x.RowVersion).OriginalValue = dto.RowVersion;

        //    try
        //    {
        //        await _db.SaveChangesAsync();
        //        return Ok(org);
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        return Conflict("This record was modified by another user.");
        //    }
        //}
    }
}

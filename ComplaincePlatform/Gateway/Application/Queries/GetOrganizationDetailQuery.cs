using Gateway.DTOs;
using MediatR;

namespace Gateway.Application.Queries;

/// <summary>
/// Query to retrieve complete organization detail including hospitals and members.
/// </summary>
public class GetOrganizationDetailQuery : IRequest<OrganizationDetailDto>
{
    public Guid TenantId { get; set; }

    public GetOrganizationDetailQuery(Guid tenantId)
    {
        TenantId = tenantId;
    }
}

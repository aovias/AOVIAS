using ComplianceService.Application.DTOs;
using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IFindingAdapter
{
    AuditFindingDto ToAuditDto(Finding finding);
    FindingListItemDto ToManagedListDto(Finding finding);
    FindingDetailDto ToManagedDetailDto(Finding finding);
}

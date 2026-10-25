using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;

namespace OrganizationService.Application.Services
{
    public class CommonLookupAppService
    {
        private readonly IDepartmentRepository _departmentRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IMasterDepartmentRepository _masterDepartmentRepository;

        public CommonLookupAppService(
            IDepartmentRepository departmentRepository,
            IServiceRepository serviceRepository,
            IMasterDepartmentRepository masterDepartmentRepository)
        {
            _departmentRepository = departmentRepository;
            _serviceRepository = serviceRepository;
            _masterDepartmentRepository = masterDepartmentRepository;
        }

        public async Task<IReadOnlyList<DepartmentDto>> GetActiveDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _departmentRepository.GetActiveAsync(cancellationToken);

            return entities
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Code = d.Code,
                    Category = d.Category
                })
                .ToList();
        }

        public async Task<IReadOnlyList<MasterDepartmentDto>> GetMasterDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _masterDepartmentRepository.GetAllAsync(cancellationToken);

            return entities
                .Select(d => new MasterDepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Code = d.Code,
                    Category = d.Category,
                    DisplayOrder = d.DisplayOrder
                })
                .ToList();
        }

        public async Task<IReadOnlyList<ServiceDto>> GetActiveServicesAsync(Guid? departmentId = null, CancellationToken cancellationToken = default)
        {
            var entities = departmentId.HasValue
                ? await _serviceRepository.GetActiveByDepartmentAsync(departmentId.Value, cancellationToken)
                : await _serviceRepository.GetActiveAsync(cancellationToken);

            return entities
                .Select(s => new ServiceDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Category = s.Category,
                    DepartmentId = s.DepartmentId,
                    IsCritical = s.IsCritical
                })
                .ToList();
        }
    }
}

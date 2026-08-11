using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using Shared.Infrastructure.Services;

namespace ComplianceService.Application.Services
{
    public class RegulationAppService
    {
        private readonly IRegulationRepository _repo;
        private readonly IUnitOfWork _uow;
        private readonly PlatformIdGenerator _idGen;

        public RegulationAppService(
            IRegulationRepository repo,
            IUnitOfWork uow,
            PlatformIdGenerator idGen)
        {
            _repo = repo;
            _uow = uow;
            _idGen = idGen;
        }

        //public async Task<Regulation> CreateAsync(CreateRegulationDto dto, Guid userId)
        //{
        //    var regulation = new Regulation
        //    {
        //        Id = Guid.NewGuid(),
        //        PlatformId = await _idGen.GenerateAsync("REG"),
        //        OrganizationId = dto.OrganizationId,
        //        Name = dto.Name,
        //        Authority = dto.Authority,
        //        Version = dto.Version,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    await _repo.AddAsync(regulation);
        //    await _uow.SaveChangesAsync();
        //    return regulation;
        //}
    }
}

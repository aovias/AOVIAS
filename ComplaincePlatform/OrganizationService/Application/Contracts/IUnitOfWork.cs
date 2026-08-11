namespace OrganizationService.Application.Contracts
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}

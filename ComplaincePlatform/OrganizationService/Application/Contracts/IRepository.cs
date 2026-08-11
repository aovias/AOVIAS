namespace OrganizationService.Application.Contracts
{
    public interface IRepository<T> where T : class
    {
        Task<T?> GetAsync(Guid id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
        IQueryable<T> Query();
    }
}

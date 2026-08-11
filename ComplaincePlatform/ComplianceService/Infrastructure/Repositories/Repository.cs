using System.Linq.Expressions;
using System.Reflection;
using ComplianceService.Application.Contracts;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ComplianceDbContext _db;
    protected readonly IUserContextService? _userContext;

    public Repository(ComplianceDbContext db, IUserContextService? userContext = null)
    {
        _db = db;
        _userContext = userContext;
    }

    public async Task<T?> GetAsync(Guid id)
    {
        var query = _db.Set<T>().AsQueryable();
        
        // Apply hospital_id filter if entity has HospitalId property
        query = ApplyHospitalFilter(query);
        
        // For FindAsync, we need to check after loading
        var entity = await _db.Set<T>().FindAsync(id);
        if (entity != null && HasHospitalIdProperty(entity))
        {
            var hospitalId = GetHospitalId(entity);
            if (_userContext != null && hospitalId != _userContext.HospitalId)
            {
                return null; // Entity belongs to different hospital
            }
        }
        
        return entity;
    }

    public async Task AddAsync(T entity) => await _db.Set<T>().AddAsync(entity);

    public void Update(T entity) => _db.Set<T>().Update(entity);

    public void Remove(T entity) => _db.Set<T>().Remove(entity);

    public IQueryable<T> Query()
    {
        var query = _db.Set<T>().AsQueryable();
        return ApplyHospitalFilter(query);
    }

    /// <summary>
    /// Applies hospital_id filter to query if entity has HospitalId property
    /// </summary>
    protected IQueryable<T> ApplyHospitalFilter(IQueryable<T> query)
    {
        if (_userContext == null) return query;

        // Check if T has HospitalId property
        var hospitalIdProperty = typeof(T).GetProperty("HospitalId", BindingFlags.Public | BindingFlags.Instance);
        if (hospitalIdProperty != null && hospitalIdProperty.PropertyType == typeof(Guid))
        {
            var hospitalId = _userContext.HospitalId;
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, hospitalIdProperty);
            var constant = Expression.Constant(hospitalId);
            var equals = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(equals, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    private bool HasHospitalIdProperty(T entity)
    {
        return typeof(T).GetProperty("HospitalId", BindingFlags.Public | BindingFlags.Instance) != null;
    }

    private Guid GetHospitalId(T entity)
    {
        var property = typeof(T).GetProperty("HospitalId", BindingFlags.Public | BindingFlags.Instance);
        if (property != null && property.GetValue(entity) is Guid hospitalId)
        {
            return hospitalId;
        }
        return Guid.Empty;
    }
}

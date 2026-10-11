using System.Linq.Expressions;
using ECommerce.Domain.Common;

namespace ECommerce.Domain.Interfaces;

public interface IGenericRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    IQueryable<T> Query();

    // Soft delete query helpers (utilizing IgnoreQueryFilters)
    Task<T?> GetByIdIncludingDeletedAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindIncludingDeletedAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetOnlyDeletedAsync(CancellationToken cancellationToken = default);
    void Restore(T entity);
    void HardDelete(T entity);
}

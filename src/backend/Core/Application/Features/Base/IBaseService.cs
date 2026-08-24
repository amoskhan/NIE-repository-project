using System.Linq.Expressions;
using Domain.Models;

namespace Application.Features;

//NOTE: DO NOT CHANGE THIS SERVICE
public interface IBaseService<T> where T : BaseEntity
{
    Task<IList<T>> GetAllAsync();

    Task<IList<T>> GetAllByIdsAsync(IEnumerable<Guid> ids);

    Task<IList<T>> GetAllByIdsAsync(IEnumerable<Guid> ids, params Expression<Func<T, object>>[] includeExpressions);

    Task<T?> GetByIdAsync(Guid id);

    Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includeExpressions);

    Task<T> SaveAsync(T entity);

    Task<T> SaveOrUpdateAsync(T entity);

    Task<bool> DeleteAsync(Guid id);
}

using System.Linq.Expressions;
using Domain.Models;

namespace Services.Services;

//NOTE: DO NOT CHANGE THIS SERVICE
public interface IBaseService<T> where T : BaseEntity
{
    Task<IList<T>> GetAllAsync();

    Task<IList<T>> GetAllByIdsAsync(IEnumerable<int> ids);

    Task<IList<T>> GetAllByIdsAsync(IEnumerable<int> ids, params Expression<Func<T, object>>[] includeExpressions);

    Task<T?> GetByIdAsync(int id);

    Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includeExpressions);

    Task<T> SaveAsync(T entity);

    Task<T> SaveOrUpdateAsync(T entity);

    Task<bool> DeleteAsync(int id);
}

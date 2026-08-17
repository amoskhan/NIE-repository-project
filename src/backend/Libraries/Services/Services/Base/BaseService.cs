using System.Linq.Expressions;
using Data.Data;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Services.Services;

//NOTE: DO NOT CHANGE THIS SERVICE
public class BaseService<T> : IBaseService<T> where T : BaseEntity
{
    protected readonly MainDbContext Context;

    public BaseService(MainDbContext context)
    {
        Context = context;
    }

    protected virtual DbSet<T> Records => Context.Set<T>();

    protected virtual IQueryable<T> Include(params Expression<Func<T, object>>[] includeExpressions)
    {
        IQueryable<T> query = Records;
        foreach (var includeExpression in includeExpressions)
        {
            query = query.Include(includeExpression);
        }
        return query;
    }

    public virtual async Task<IList<T>> GetAllAsync()
    {
        return await Records.ToListAsync();
    }

    public virtual async Task<IList<T>> GetAllByIdsAsync(IEnumerable<int> ids)
    {
        return await Records.Where(entity => ids.Contains(entity.Id)).ToListAsync();
    }

    public virtual async Task<IList<T>> GetAllByIdsAsync(IEnumerable<int> ids, params Expression<Func<T, object>>[] includeExpressions)
    {
        var query = Include(includeExpressions);
        return await query.Where(entity => ids.Contains(entity.Id)).ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await Records.FindAsync(id);
    }

    public virtual async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includeExpressions)
    {
        var query = Include(includeExpressions);
        return await query.FirstOrDefaultAsync(entity => entity.Id == id);
    }

    public virtual async Task<T> SaveAsync(T entity)
    {
        await Records.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> SaveOrUpdateAsync(T entity)
    {
        Records.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await Records.FindAsync(id);
        if (entity == null)
            return false;

        Records.Remove(entity);
        await Context.SaveChangesAsync();
        return true;
    }
}

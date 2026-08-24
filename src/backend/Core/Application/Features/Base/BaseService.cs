using System.Linq.Expressions;
using Application.Abstractions;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Features;

//NOTE: DO NOT CHANGE THIS SERVICE
public class BaseService<T> : IBaseService<T> where T : BaseEntity
{
    protected readonly IApplicationDbContext Context;

    public BaseService(IApplicationDbContext context)
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

    public virtual async Task<IList<T>> GetAllByIdsAsync(IEnumerable<Guid> ids)
    {
        return await Records.Where(entity => ids.Contains(entity.Id)).ToListAsync();
    }

    public virtual async Task<IList<T>> GetAllByIdsAsync(IEnumerable<Guid> ids, params Expression<Func<T, object>>[] includeExpressions)
    {
        var query = Include(includeExpressions);
        return await query.Where(entity => ids.Contains(entity.Id)).ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id)
    {
        return await Records.FindAsync(id);
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includeExpressions)
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

    public virtual async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await Records.FindAsync(id);
        if (entity == null)
            return false;

        Records.Remove(entity);
        await Context.SaveChangesAsync();
        return true;
    }
}

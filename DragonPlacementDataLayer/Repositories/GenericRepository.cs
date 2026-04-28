using System.Linq.Expressions;
using DragonPlacementDataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace DragonPlacementDataLayer.Repositories;

public enum DeleteResult
{
    Deleted = 1,
    NotFound = 2
}

public interface IGenericRepository<TEntity> where TEntity : class
{
    DeleteResult Delete(object id);
    void Delete(TEntity entityToDelete);
    IEnumerable<TEntity> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = ""
    );
    Task<TEntity?> GetByID(object id);
    void Insert(TEntity entity);
    Task<int> SaveChangesAsync();
    void Update(TEntity entityToUpdate);
}

public class GenericRepository<TEntity>(DragonPlacementContext context) : IGenericRepository<TEntity> where TEntity : class
{
    internal readonly DragonPlacementContext _context = context;
    internal readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public virtual IEnumerable<TEntity> Get(
        Expression<Func<TEntity, bool>> filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        string includeProperties = "")
    {
        IQueryable<TEntity> query = _dbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        foreach (var includeProperty in includeProperties.Split([','], StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProperty);
        }

        return orderBy == null
            ? query
            : orderBy(query);
    }

    public virtual async Task<TEntity?> GetByID(object id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual void Insert(TEntity entity)
    {
        _dbSet.Add(entity);
    }

    public virtual DeleteResult Delete(object id)
    {
        TEntity? entityToDelete = _dbSet.Find(id);
        if (entityToDelete == null)
        {
            return DeleteResult.NotFound;
        }
        else
        {
            Delete(entityToDelete);
            return DeleteResult.Deleted;
        }
    }

    public virtual void Delete(TEntity entityToDelete)
    {
        if (_context.Entry(entityToDelete).State == EntityState.Detached)
        {
            _dbSet.Attach(entityToDelete);
        }
        _dbSet.Remove(entityToDelete);
    }

    public virtual void Update(TEntity entityToUpdate)
    {
        _dbSet.Attach(entityToUpdate);
        _context.Entry(entityToUpdate).State = EntityState.Modified;
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}

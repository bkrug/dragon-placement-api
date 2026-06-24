using System.Linq.Expressions;

namespace DragonCommonApplication.Repositories;

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
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string includeProperties = ""
    );
    Task<TEntity?> GetByID(object id);
    void Insert(TEntity entity);
    Task<int> SaveChangesAsync();
    void Update(TEntity entityToUpdate);
}

using System.Linq.Expressions;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(object id);
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null);
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }
}


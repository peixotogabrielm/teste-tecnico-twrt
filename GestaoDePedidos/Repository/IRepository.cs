using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Entities;

namespace GestaoDePedidos.Repository;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<PagedResult<T>> GetPagedAsync(PagedRequest request);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}

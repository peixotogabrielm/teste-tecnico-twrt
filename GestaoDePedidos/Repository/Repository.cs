using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Data;
using GestaoDePedidos.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.Repository;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(ApplicationDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id) => await DbSet.FindAsync(id);

    public async Task<IReadOnlyList<T>> GetAllAsync() => await DbSet.AsNoTracking().ToListAsync();

    public async Task<PagedResult<T>> GetPagedAsync(PagedRequest request)
    {
        var query = DbSet.AsNoTracking();

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResult<T>(items, request.PageNumber, request.PageSize, totalCount);
    }

    public async Task AddAsync(T entity) => await DbSet.AddAsync(entity);

    public void Update(T entity) => DbSet.Update(entity);

    public void Remove(T entity) => DbSet.Remove(entity);

    public async Task<int> SaveChangesAsync() => await Context.SaveChangesAsync();
}

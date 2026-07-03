using GestaoDePedidos.Data;
using GestaoDePedidos.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.Repository;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAtivoComEmailAsync(string email, Guid? excludeId = null) =>
        await DbSet.AnyAsync(c => c.Ativo && c.Email == email && c.Id != excludeId);

    public async Task<bool> ExistsAtivoComDocumentoAsync(string documento, Guid? excludeId = null) =>
        await DbSet.AnyAsync(c => c.Ativo && c.Documento == documento && c.Id != excludeId);
}

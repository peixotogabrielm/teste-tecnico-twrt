using GestaoDePedidos.Data;
using GestaoDePedidos.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.Repository;

public class ClienteRepository : Repository<Cliente>, IClienteRepository
{
    public ClienteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsAtivoComEmailAsync(string email) =>
        await DbSet.AnyAsync(c => c.Ativo && c.Email == email);

    public async Task<bool> ExistsAtivoComDocumentoAsync(string documento) =>
        await DbSet.AnyAsync(c => c.Ativo && c.Documento == documento);
}

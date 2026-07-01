using GestaoDePedidos.Data;
using GestaoDePedidos.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.Repository;

public class UsuarioRepository : Repository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Usuario?> GetByEmailAsync(string email) =>
        await DbSet.FirstOrDefaultAsync(u => u.Email == email);
}

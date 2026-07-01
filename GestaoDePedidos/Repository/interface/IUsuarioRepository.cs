using GestaoDePedidos.Entities;

namespace GestaoDePedidos.Repository;

public interface IUsuarioRepository : IRepository<Usuario>
{
    Task<Usuario?> GetByEmailAsync(string email);
}

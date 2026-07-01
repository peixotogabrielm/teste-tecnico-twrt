using GestaoDePedidos.Entities;

namespace GestaoDePedidos.Repository;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<bool> ExistsAtivoComEmailAsync(string email);
    Task<bool> ExistsAtivoComDocumentoAsync(string documento);
}

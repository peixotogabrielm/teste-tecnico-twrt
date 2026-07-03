using GestaoDePedidos.Entities;

namespace GestaoDePedidos.Repository;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<bool> ExistsAtivoComEmailAsync(string email, Guid? excludeId = null);
    Task<bool> ExistsAtivoComDocumentoAsync(string documento, Guid? excludeId = null);
}

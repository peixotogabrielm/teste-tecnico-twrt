using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Dtos.Clientes;

namespace GestaoDePedidos.Services;

public interface IClienteService
{
    Task<ClienteResponse> CriarAsync(CreateClienteRequest request);
    Task<PagedResult<ClienteResponse>> ObterPaginadoAsync(PagedRequest request);
    Task<ClienteResponse> ObterPorIdAsync(Guid id);
    Task AtualizarStatusAsync(Guid id, bool ativo);
}

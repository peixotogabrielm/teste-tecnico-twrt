using GestaoDePedidos.Dtos.Pedidos;

namespace GestaoDePedidos.Services;

public interface IPedidoService
{
    Task<PedidoResponse> CriarAsync(CriarPedidoRequest request);
    Task<PedidoResponse> ObterPorIdAsync(Guid id);
    Task AtualizarStatusAsync(Guid id, AtualizarPedidoStatusRequest request);
}

using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Dtos.Pedidos;

public class PedidoResponse
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public PedidoStatus Status { get; set; }
    public DateTimeOffset DataCriacao { get; set; }
    public decimal ValorTotal { get; set; }
    public List<PedidoItemResponse> Itens { get; set; } = new();
    public List<PedidoStatusHistoricoResponse> HistoricoStatus { get; set; } = new();
}

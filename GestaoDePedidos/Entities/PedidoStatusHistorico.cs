using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Entities;

public class PedidoStatusHistorico : BaseEntity
{
    public Guid PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;

    public PedidoStatus? StatusAnterior { get; set; }
    public PedidoStatus NovoStatus { get; set; }
    public DateTime DataAlteracao { get; set; } = DateTime.UtcNow;
    public string? Motivo { get; set; }
}

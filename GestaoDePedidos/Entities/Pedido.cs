using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Entities;

public class Pedido : BaseEntity
{
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public PedidoStatus Status { get; set; } = PedidoStatus.Criado;
    public decimal ValorTotal { get; set; }

    public ICollection<PedidoItem> Itens { get; set; } = new List<PedidoItem>();
    public ICollection<PedidoStatusHistorico> HistoricoStatus { get; set; } = new List<PedidoStatusHistorico>();
}

using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Dtos.Pedidos;

public class PedidoStatusHistoricoResponse
{
    public PedidoStatus? StatusAnterior { get; set; }
    public PedidoStatus NovoStatus { get; set; }
    public DateTimeOffset DataAlteracao { get; set; }
    public string? Motivo { get; set; }
}

using System.ComponentModel.DataAnnotations;
using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Dtos.Pedidos;

public class AtualizarPedidoStatusRequest
{
    public PedidoStatus NovoStatus { get; set; }

    [StringLength(500)]
    public string? Motivo { get; set; }
}

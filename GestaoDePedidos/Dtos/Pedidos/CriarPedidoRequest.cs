using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Pedidos;

public class CriarPedidoRequest
{
    [Required(ErrorMessage = "O cliente é obrigatório.")]
    public Guid ClienteId { get; set; }

    public List<CriarPedidoItemRequest> Itens { get; set; } = new();
}

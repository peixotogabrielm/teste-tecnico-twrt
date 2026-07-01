using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Pedidos;

public class CriarPedidoItemRequest
{
    [Required(ErrorMessage = "O produto é obrigatório.")]
    public Guid ProdutoId { get; set; }

    public decimal Quantidade { get; set; }
}

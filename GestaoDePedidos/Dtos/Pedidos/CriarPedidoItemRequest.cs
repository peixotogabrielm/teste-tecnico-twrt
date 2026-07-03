using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Pedidos;

public class CriarPedidoItemRequest
{
    [Required(ErrorMessage = "O produto é obrigatório.")]
    public Guid ProdutoId { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Quantidade { get; set; }
}

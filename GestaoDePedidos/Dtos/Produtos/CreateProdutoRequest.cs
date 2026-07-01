using System.ComponentModel.DataAnnotations;

namespace GestaoDePedidos.Dtos.Produtos;

public class CreateProdutoRequest
{
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Descricao { get; set; }

    public decimal Preco { get; set; }

    public decimal EstoqueDisponivel { get; set; }

    [Required(ErrorMessage = "A unidade de medida é obrigatória.")]
    [StringLength(10)]
    public string UnidadeMedida { get; set; } = string.Empty;

    [Required(ErrorMessage = "O campo PermiteVendaFracionada é obrigatório.")]
    public bool PermiteVendaFracionada { get; set; }
}

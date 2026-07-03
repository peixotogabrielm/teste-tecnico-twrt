using System.ComponentModel.DataAnnotations;
using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Dtos.Produtos;

public class UpdateProdutoEstoqueRequest
{
    public TipoMovimentacaoEstoque Tipo { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", MinimumIsExclusive = true, ErrorMessage = "A quantidade deve ser maior que zero.")]
    public decimal Quantidade { get; set; }
}

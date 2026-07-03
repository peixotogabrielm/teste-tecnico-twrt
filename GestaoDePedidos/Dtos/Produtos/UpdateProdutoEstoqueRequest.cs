using GestaoDePedidos.Enums;

namespace GestaoDePedidos.Dtos.Produtos;

public class UpdateProdutoEstoqueRequest
{
    public TipoMovimentacaoEstoque Tipo { get; set; }
    public decimal Quantidade { get; set; }
}

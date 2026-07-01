using GestaoDePedidos.Entities;

namespace GestaoDePedidos.Dtos.Produtos;

public static class ProdutoExtensions
{
    public static ProdutoResponse ToResponse(this Produto produto) => new()
    {
        Id = produto.Id,
        Nome = produto.Nome,
        Descricao = produto.Descricao,
        Preco = produto.Preco,
        EstoqueDisponivel = produto.EstoqueDisponivel,
        UnidadeMedida = produto.UnidadeMedida,
        Ativo = produto.Ativo,
        PermiteVendaFracionada = produto.PermiteVendaFracionada,
        CreatedAt = produto.DataCriacao,
        UpdatedAt = produto.DataAtualizacao
    };
}

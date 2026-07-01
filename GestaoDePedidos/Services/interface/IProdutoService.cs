using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Dtos.Produtos;

namespace GestaoDePedidos.Services;

public interface IProdutoService
{
    Task<ProdutoResponse> CriarAsync(CreateProdutoRequest request);
    Task<PagedResult<ProdutoResponse>> ObterPaginadoAsync(PagedRequest request);
    Task<ProdutoResponse> ObterPorIdAsync(Guid id);
    Task AtualizarAsync(Guid id, UpdateProdutoRequest request);
    Task AtualizarStatusAsync(Guid id, bool ativo);
    Task AtualizarEstoqueAsync(Guid id, decimal estoqueDisponivel);
}

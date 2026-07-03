using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Common.Validation;
using GestaoDePedidos.Dtos.Produtos;
using GestaoDePedidos.Entities;
using GestaoDePedidos.Enums;
using GestaoDePedidos.Repository;

namespace GestaoDePedidos.Services;

public class ProdutoService : IProdutoService
{
    private readonly IRepository<Produto> _produtoRepository;

    public ProdutoService(IRepository<Produto> produtoRepository)
    {
        _produtoRepository = produtoRepository;
    }

    public async Task<ProdutoResponse> CriarAsync(CreateProdutoRequest request)
    {
        if (request.Preco <= 0)
        {
            throw new BadRequestException("O preço deve ser maior que zero.");
        }

        if (!ValorMonetarioValidator.IsValid(request.Preco))
        {
            throw new BadRequestException("O preço pode ter no máximo 2 casas decimais.");
        }

        if (request.EstoqueDisponivel < 0)
        {
            throw new BadRequestException("O estoque disponível não pode ser negativo.");
        }

        ValidarCasasDecimaisEstoque(request.EstoqueDisponivel, request.PermiteVendaFracionada);

        var produto = new Produto
        {
            Nome = request.Nome,
            Descricao = request.Descricao,
            Preco = request.Preco,
            EstoqueDisponivel = request.EstoqueDisponivel,
            UnidadeMedida = request.UnidadeMedida,
            PermiteVendaFracionada = request.PermiteVendaFracionada,
            Ativo = true
        };

        await _produtoRepository.AddAsync(produto);
        await _produtoRepository.SaveChangesAsync();

        return produto.ToResponse();
    }

    public async Task<PagedResult<ProdutoResponse>> ObterPaginadoAsync(PagedRequest request)
    {
        var pagedProdutos = await _produtoRepository.GetPagedAsync(request);
        var items = pagedProdutos.Items.Select(p => p.ToResponse()).ToList();

        return new PagedResult<ProdutoResponse>(items, pagedProdutos.PageNumber, pagedProdutos.PageSize, pagedProdutos.TotalCount);
    }

    public async Task<ProdutoResponse> ObterPorIdAsync(Guid id)
    {
        var produto = await _produtoRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Produto não encontrado.");

        return produto.ToResponse();
    }

    public async Task AtualizarAsync(Guid id, UpdateProdutoRequest request)
    {
        var produto = await _produtoRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Produto não encontrado.");

        if (request.Preco <= 0)
        {
            throw new BadRequestException("O preço deve ser maior que zero.");
        }

        if (!ValorMonetarioValidator.IsValid(request.Preco))
        {
            throw new BadRequestException("O preço pode ter no máximo 2 casas decimais.");
        }

        var desabilitandoVendaFracionada = produto.PermiteVendaFracionada && !request.PermiteVendaFracionada;
        if (desabilitandoVendaFracionada && !QuantidadeValidator.IsValid(produto.EstoqueDisponivel, permiteVendaFracionada: false))
        {
            throw new BadRequestException("Não é possível desabilitar a venda fracionada: o estoque atual possui valor fracionado.");
        }

        produto.Nome = request.Nome;
        produto.Descricao = request.Descricao;
        produto.Preco = request.Preco;
        produto.UnidadeMedida = request.UnidadeMedida;
        produto.PermiteVendaFracionada = request.PermiteVendaFracionada;
        produto.DataAtualizacao = DateTime.UtcNow;

        _produtoRepository.Update(produto);
        await _produtoRepository.SaveChangesAsync();
    }

    public async Task AtualizarStatusAsync(Guid id, bool ativo)
    {
        var produto = await _produtoRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Produto não encontrado.");

        produto.Ativo = ativo;
        produto.DataAtualizacao = DateTime.UtcNow;

        _produtoRepository.Update(produto);
        await _produtoRepository.SaveChangesAsync();
    }

    public async Task AtualizarEstoqueAsync(Guid id, TipoMovimentacaoEstoque tipo, decimal quantidade)
    {
        var produto = await _produtoRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Produto não encontrado.");

        if (quantidade <= 0)
        {
            throw new BadRequestException("A quantidade deve ser maior que zero.");
        }

        if (!QuantidadeValidator.IsValid(quantidade, produto.PermiteVendaFracionada))
        {
            throw new BadRequestException(produto.PermiteVendaFracionada
                ? "A quantidade pode ter no máximo 3 casas decimais."
                : "A quantidade deve ser um valor inteiro para produtos que não permitem venda fracionada.");
        }

        if (tipo == TipoMovimentacaoEstoque.Entrada)
        {
            produto.EstoqueDisponivel += quantidade;
        }
        else
        {
            if (quantidade > produto.EstoqueDisponivel)
            {
                throw new BadRequestException("Estoque insuficiente para o produto informado.");
            }

            produto.EstoqueDisponivel -= quantidade;
        }

        produto.DataAtualizacao = DateTime.UtcNow;

        _produtoRepository.Update(produto);
        await _produtoRepository.SaveChangesAsync();
    }

    private static void ValidarCasasDecimaisEstoque(decimal estoqueDisponivel, bool permiteVendaFracionada)
    {
        if (QuantidadeValidator.IsValid(estoqueDisponivel, permiteVendaFracionada))
        {
            return;
        }

        throw new BadRequestException(permiteVendaFracionada
            ? "O estoque disponível pode ter no máximo 3 casas decimais."
            : "O estoque disponível deve ser um valor inteiro para produtos que não permitem venda fracionada.");
    }
}

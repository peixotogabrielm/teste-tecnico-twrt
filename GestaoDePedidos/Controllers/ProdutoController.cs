using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Dtos.Produtos;
using GestaoDePedidos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoDePedidos.Controllers;

/// <summary>Catálogo de produtos. Requer autenticação de Admin.</summary>
[ApiController]
[Route("api/produtos")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
public class ProdutoController : ControllerBase
{
    private readonly IProdutoService _produtoService;

    public ProdutoController(IProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    /// <summary>Cadastra um novo produto.</summary>
    /// <response code="201">Produto criado.</response>
    /// <response code="400">Preço inválido, estoque negativo, ou estoque fracionado num produto que não permite venda fracionada.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Criar([FromBody] CreateProdutoRequest request)
    {
        var produto = await _produtoService.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, produto);
    }

    /// <summary>Lista produtos de forma paginada.</summary>
    /// <response code="200">Página de produtos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProdutoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] PagedRequest request)
    {
        var resultado = await _produtoService.ObterPaginadoAsync(request);
        return Ok(resultado);
    }

    /// <summary>Busca um produto pelo id.</summary>
    /// <response code="200">Produto encontrado.</response>
    /// <response code="404">Nenhum produto com esse id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var produto = await _produtoService.ObterPorIdAsync(id);
        return Ok(produto);
    }

    /// <summary>Atualiza os dados cadastrais de um produto (nome, descrição, preço, unidade, venda fracionada).</summary>
    /// <response code="204">Produto atualizado.</response>
    /// <response code="400">Preço inválido, ou tentativa de desabilitar venda fracionada com estoque atual fracionado.</response>
    /// <response code="404">Nenhum produto com esse id.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] UpdateProdutoRequest request)
    {
        await _produtoService.AtualizarAsync(id, request);
        return NoContent();
    }

    /// <summary>Ativa ou inativa um produto.</summary>
    /// <response code="204">Status atualizado.</response>
    /// <response code="404">Nenhum produto com esse id.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] UpdateProdutoStatusRequest request)
    {
        await _produtoService.AtualizarStatusAsync(id, request.Ativo);
        return NoContent();
    }

    /// <summary>Ajusta manualmente o estoque disponível de um produto.</summary>
    /// <response code="204">Estoque atualizado.</response>
    /// <response code="400">Estoque negativo, ou número de casas decimais incompatível com a venda fracionada do produto.</response>
    /// <response code="404">Nenhum produto com esse id.</response>
    [HttpPatch("{id:guid}/estoque")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarEstoque(Guid id, [FromBody] UpdateProdutoEstoqueRequest request)
    {
        await _produtoService.AtualizarEstoqueAsync(id, request.EstoqueDisponivel);
        return NoContent();
    }
}

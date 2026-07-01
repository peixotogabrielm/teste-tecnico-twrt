using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Dtos.Produtos;
using GestaoDePedidos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoDePedidos.Controllers;

[ApiController]
[Route("api/produtos")]
[Authorize(Roles = "Admin")]
public class ProdutoController : ControllerBase
{
    private readonly IProdutoService _produtoService;

    public ProdutoController(IProdutoService produtoService)
    {
        _produtoService = produtoService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CreateProdutoRequest request)
    {
        var produto = await _produtoService.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, produto);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProdutoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] PagedRequest request)
    {
        var resultado = await _produtoService.ObterPaginadoAsync(request);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProdutoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var produto = await _produtoService.ObterPorIdAsync(id);
        return Ok(produto);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] UpdateProdutoRequest request)
    {
        await _produtoService.AtualizarAsync(id, request);
        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] UpdateProdutoStatusRequest request)
    {
        await _produtoService.AtualizarStatusAsync(id, request.Ativo);
        return NoContent();
    }

    [HttpPatch("{id:guid}/estoque")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AtualizarEstoque(Guid id, [FromBody] UpdateProdutoEstoqueRequest request)
    {
        await _produtoService.AtualizarEstoqueAsync(id, request.EstoqueDisponivel);
        return NoContent();
    }
}

using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Common.Responses;
using GestaoDePedidos.Dtos.Pedidos;
using GestaoDePedidos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoDePedidos.Controllers;

/// <summary>Ciclo de vida de pedidos: criação, consulta e transição de status. Requer autenticação de Admin.</summary>
[ApiController]
[Route("api/pedidos")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public class PedidoController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidoController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    /// <summary>Cria um pedido para um cliente, baixando o estoque de cada produto atomicamente.</summary>
    /// <response code="201">Pedido criado.</response>
    /// <response code="400">Cliente/produto inativo, item duplicado, quantidade inválida, ou estoque insuficiente.</response>
    /// <response code="404">Cliente ou produto informado não existe.</response>
    /// <response code="409">Conflito de concorrência: outra requisição consumiu o estoque disputado primeiro.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar([FromBody] CriarPedidoRequest request)
    {
        var pedido = await _pedidoService.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = pedido.Id }, pedido);
    }

    /// <summary>Lista pedidos de forma paginada, opcionalmente filtrando por cliente.</summary>
    /// <response code="200">Página de pedidos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PedidoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] PagedRequest request, [FromQuery] Guid? clienteId)
    {
        var resultado = await _pedidoService.ObterPaginadoAsync(request, clienteId);
        return Ok(resultado);
    }

    /// <summary>Busca um pedido pelo id, incluindo itens e histórico de status.</summary>
    /// <response code="200">Pedido encontrado.</response>
    /// <response code="404">Nenhum pedido com esse id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var pedido = await _pedidoService.ObterPorIdAsync(id);
        return Ok(pedido);
    }

    /// <summary>Transiciona o status do pedido (ex.: Criado → Pago, Criado → Cancelado). Cancelamento restaura o estoque.</summary>
    /// <response code="204">Status atualizado.</response>
    /// <response code="400">Transição de status inválida para o estado atual do pedido.</response>
    /// <response code="404">Nenhum pedido com esse id.</response>
    /// <response code="409">Outra requisição alterou o status do pedido primeiro.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarPedidoStatusRequest request)
    {
        await _pedidoService.AtualizarStatusAsync(id, request);
        return NoContent();
    }
}

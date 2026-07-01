using GestaoDePedidos.Dtos.Pedidos;
using GestaoDePedidos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoDePedidos.Controllers;

[ApiController]
[Route("api/pedidos")]
[Authorize(Roles = "Admin")]
public class PedidoController : ControllerBase
{
    private readonly IPedidoService _pedidoService;

    public PedidoController(IPedidoService pedidoService)
    {
        _pedidoService = pedidoService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CriarPedidoRequest request)
    {
        var pedido = await _pedidoService.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = pedido.Id }, pedido);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var pedido = await _pedidoService.ObterPorIdAsync(id);
        return Ok(pedido);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] AtualizarPedidoStatusRequest request)
    {
        await _pedidoService.AtualizarStatusAsync(id, request);
        return NoContent();
    }
}

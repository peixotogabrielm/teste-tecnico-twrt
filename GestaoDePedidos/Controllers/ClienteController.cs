using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Dtos.Clientes;
using GestaoDePedidos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoDePedidos.Controllers;

[ApiController]
[Route("api/clientes")]
[Authorize(Roles = "Admin")]
public class ClienteController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClienteController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Criar([FromBody] CreateClienteRequest request)
    {
        var cliente = await _clienteService.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id }, cliente);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClienteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] PagedRequest request)
    {
        var resultado = await _clienteService.ObterPaginadoAsync(request);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var cliente = await _clienteService.ObterPorIdAsync(id);
        return Ok(cliente);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] UpdateClienteStatusRequest request)
    {
        await _clienteService.AtualizarStatusAsync(id, request.Ativo);
        return NoContent();
    }
}

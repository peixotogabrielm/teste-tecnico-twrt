using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Common.Responses;
using GestaoDePedidos.Dtos.Clientes;
using GestaoDePedidos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestaoDePedidos.Controllers;

/// <summary>Cadastro de clientes. Requer autenticação de Admin.</summary>
[ApiController]
[Route("api/clientes")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
public class ClienteController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClienteController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    /// <summary>Cadastra um novo cliente.</summary>
    /// <response code="201">Cliente criado.</response>
    /// <response code="400">Documento (CPF/CNPJ) inválido ou dados obrigatórios ausentes.</response>
    /// <response code="409">Já existe um cliente ativo com o mesmo e-mail ou documento.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Criar([FromBody] CreateClienteRequest request)
    {
        var cliente = await _clienteService.CriarAsync(request);
        return CreatedAtAction(nameof(ObterPorId), new { id = cliente.Id }, cliente);
    }

    /// <summary>Lista clientes de forma paginada.</summary>
    /// <response code="200">Página de clientes.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ClienteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] PagedRequest request)
    {
        var resultado = await _clienteService.ObterPaginadoAsync(request);
        return Ok(resultado);
    }

    /// <summary>Busca um cliente pelo id.</summary>
    /// <response code="200">Cliente encontrado.</response>
    /// <response code="404">Nenhum cliente com esse id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var cliente = await _clienteService.ObterPorIdAsync(id);
        return Ok(cliente);
    }

    /// <summary>Ativa ou inativa um cliente.</summary>
    /// <response code="204">Status atualizado.</response>
    /// <response code="404">Nenhum cliente com esse id.</response>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AtualizarStatus(Guid id, [FromBody] UpdateClienteStatusRequest request)
    {
        await _clienteService.AtualizarStatusAsync(id, request.Ativo);
        return NoContent();
    }
}

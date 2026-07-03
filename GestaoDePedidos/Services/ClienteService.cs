using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Common.Validation;
using GestaoDePedidos.Dtos.Clientes;
using GestaoDePedidos.Entities;
using GestaoDePedidos.Repository;

namespace GestaoDePedidos.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clienteRepository;

    public ClienteService(IClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    public async Task<ClienteResponse> CriarAsync(CreateClienteRequest request)
    {
        if (!DocumentoValidator.IsValid(request.Documento))
        {
            throw new BadRequestException("Documento inválido. Informe um CPF ou CNPJ válido.");
        }

        var email = EmailNormalizer.Normalizar(request.Email);
        var documento = DocumentoValidator.Normalizar(request.Documento);

        if (await _clienteRepository.ExistsAtivoComEmailAsync(email))
        {
            throw new ConflictException("Já existe um cliente ativo com este e-mail.");
        }

        if (await _clienteRepository.ExistsAtivoComDocumentoAsync(documento))
        {
            throw new ConflictException("Já existe um cliente ativo com este documento.");
        }

        var cliente = new Cliente
        {
            Nome = request.Nome,
            Email = email,
            Documento = documento,
            Ativo = true
        };

        await _clienteRepository.AddAsync(cliente);
        await _clienteRepository.SaveChangesAsync();

        return cliente.ToResponse();
    }

    public async Task<PagedResult<ClienteResponse>> ObterPaginadoAsync(PagedRequest request)
    {
        var pagedClientes = await _clienteRepository.GetPagedAsync(request);
        var items = pagedClientes.Items.Select(c => c.ToResponse()).ToList();

        return new PagedResult<ClienteResponse>(items, pagedClientes.PageNumber, pagedClientes.PageSize, pagedClientes.TotalCount);
    }

    public async Task<ClienteResponse> ObterPorIdAsync(Guid id)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Cliente não encontrado.");

        return cliente.ToResponse();
    }

    public async Task AtualizarStatusAsync(Guid id, bool ativo)
    {
        var cliente = await _clienteRepository.GetByIdAsync(id)
            ?? throw new NotFoundException("Cliente não encontrado.");

        if (ativo)
        {
            if (await _clienteRepository.ExistsAtivoComEmailAsync(cliente.Email, cliente.Id))
            {
                throw new ConflictException("Já existe um cliente ativo com este e-mail.");
            }

            if (await _clienteRepository.ExistsAtivoComDocumentoAsync(cliente.Documento, cliente.Id))
            {
                throw new ConflictException("Já existe um cliente ativo com este documento.");
            }
        }

        cliente.Ativo = ativo;
        cliente.DataAtualizacao = DateTime.UtcNow;

        _clienteRepository.Update(cliente);
        await _clienteRepository.SaveChangesAsync();
    }
}

using GestaoDePedidos.Entities;

namespace GestaoDePedidos.Dtos.Clientes;

public static class ClienteExtensions
{
    public static ClienteResponse ToResponse(this Cliente cliente) => new()
    {
        Id = cliente.Id,
        Nome = cliente.Nome,
        Email = cliente.Email,
        Documento = cliente.Documento,
        Ativo = cliente.Ativo,
        CreatedAt = cliente.DataCriacao,
        UpdatedAt = cliente.DataAtualizacao
    };
}

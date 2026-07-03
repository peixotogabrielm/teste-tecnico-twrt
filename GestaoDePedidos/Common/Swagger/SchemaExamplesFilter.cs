using System.Text.Json.Nodes;
using GestaoDePedidos.Common.Pagination;
using GestaoDePedidos.Dtos.Auth;
using GestaoDePedidos.Dtos.Clientes;
using GestaoDePedidos.Dtos.Pedidos;
using GestaoDePedidos.Dtos.Produtos;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GestaoDePedidos.Common.Swagger;

/// <summary>
/// Exemplos a nível de schema, um por tipo de request/response de sucesso. Todos reaproveitam a mesma
/// "história" (cliente Maria Oliveira, produtos Arroz/Leite, um pedido pago) pra que os exemplos façam
/// sentido lidos em conjunto, não só isoladamente. ApiErrorResponse não tem exemplo aqui de propósito:
/// como o mesmo tipo é usado em vários status codes diferentes por endpoint, seu exemplo é montado por
/// operação em ErrorResponseExamplesFilter.
/// </summary>
public class SchemaExamplesFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema)
        {
            return;
        }

        var example = ExampleFor(context.Type);
        if (example is not null)
        {
            concreteSchema.Example = JsonNode.Parse(example);
        }
    }

    private static string? ExampleFor(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PagedResult<>))
        {
            return PagedExampleFor(type.GenericTypeArguments[0]);
        }

        if (type == typeof(LoginRequest))
        {
            return """
                { "email": "admin@gestaopedidos.com", "senha": "SenhaForte123!" }
                """;
        }

        if (type == typeof(LoginResponse))
        {
            return """
                {
                  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJBZG1pbiJ9.4f9d6b6e6a2c1e0f8a7b6c5d4e3f2a1b0c9d8e7f6a5b4c3d2e1f0a9b8c7d6e5f",
                  "expiresAt": "2026-07-03T18:00:00Z",
                  "userId": "e1d2c3b4-a5f6-4789-9abc-def012345678",
                  "role": "Admin"
                }
                """;
        }

        if (type == typeof(CreateClienteRequest))
        {
            return """
                { "nome": "Maria Oliveira", "email": "maria.oliveira@example.com", "documento": "529.982.247-25" }
                """;
        }

        if (type == typeof(UpdateClienteStatusRequest))
        {
            return """
                { "ativo": false }
                """;
        }

        if (type == typeof(ClienteResponse))
        {
            return """
                {
                  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "nome": "Maria Oliveira",
                  "email": "maria.oliveira@example.com",
                  "documento": "529.982.247-25",
                  "ativo": true,
                  "createdAt": "2026-06-01T14:32:00Z",
                  "updatedAt": null
                }
                """;
        }

        if (type == typeof(CreateProdutoRequest))
        {
            return """
                {
                  "nome": "Arroz Tipo 1 5kg",
                  "descricao": "Pacote de arroz branco tipo 1, 5kg",
                  "preco": 29.90,
                  "estoqueDisponivel": 120.5,
                  "unidadeMedida": "kg",
                  "permiteVendaFracionada": true
                }
                """;
        }

        if (type == typeof(UpdateProdutoRequest))
        {
            return """
                {
                  "nome": "Arroz Tipo 1 5kg",
                  "descricao": "Pacote de arroz branco tipo 1, 5kg",
                  "preco": 31.90,
                  "unidadeMedida": "kg",
                  "permiteVendaFracionada": true
                }
                """;
        }

        if (type == typeof(UpdateProdutoStatusRequest))
        {
            return """
                { "ativo": false }
                """;
        }

        if (type == typeof(UpdateProdutoEstoqueRequest))
        {
            return """
                { "tipo": "Entrada", "quantidade": 50 }
                """;
        }

        if (type == typeof(ProdutoResponse))
        {
            return """
                {
                  "id": "8c2e6a10-1b3d-4e2f-9a6c-7d8e9f0a1b2c",
                  "nome": "Arroz Tipo 1 5kg",
                  "descricao": "Pacote de arroz branco tipo 1, 5kg",
                  "preco": 29.90,
                  "estoqueDisponivel": 120.5,
                  "unidadeMedida": "kg",
                  "ativo": true,
                  "permiteVendaFracionada": true,
                  "createdAt": "2026-05-10T09:00:00Z",
                  "updatedAt": "2026-06-15T11:20:00Z"
                }
                """;
        }

        if (type == typeof(CriarPedidoRequest))
        {
            return """
                {
                  "clienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "itens": [
                    { "produtoId": "8c2e6a10-1b3d-4e2f-9a6c-7d8e9f0a1b2c", "quantidade": 2.5 },
                    { "produtoId": "1b7f4d22-3c5e-4a1b-8f2d-6e9a0b1c2d3e", "quantidade": 3 }
                  ]
                }
                """;
        }

        if (type == typeof(AtualizarPedidoStatusRequest))
        {
            return """
                { "novoStatus": "Cancelado", "motivo": "Cliente desistiu da compra." }
                """;
        }

        if (type == typeof(PedidoResponse))
        {
            return """
                {
                  "id": "6fa459ea-ee8a-3ca4-894e-db77e160355e",
                  "clienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "status": "Pago",
                  "dataCriacao": "2026-06-20T10:00:00Z",
                  "valorTotal": 88.25,
                  "itens": [
                    {
                      "id": "b1e2c3d4-1111-4a2b-9c3d-4e5f6a7b8c9d",
                      "produtoId": "8c2e6a10-1b3d-4e2f-9a6c-7d8e9f0a1b2c",
                      "quantidade": 2.5,
                      "precoUnitario": 29.90,
                      "valorTotal": 74.75
                    },
                    {
                      "id": "c2d3e4f5-2222-4b3c-8d4e-5f6a7b8c9d0e",
                      "produtoId": "1b7f4d22-3c5e-4a1b-8f2d-6e9a0b1c2d3e",
                      "quantidade": 3,
                      "precoUnitario": 4.50,
                      "valorTotal": 13.50
                    }
                  ],
                  "historicoStatus": [
                    { "statusAnterior": null, "novoStatus": "Criado", "dataAlteracao": "2026-06-20T10:00:00Z", "motivo": null },
                    { "statusAnterior": "Criado", "novoStatus": "Pago", "dataAlteracao": "2026-06-20T10:05:00Z", "motivo": null }
                  ]
                }
                """;
        }

        return null;
    }

    private static string? PagedExampleFor(Type itemType)
    {
        var item = ExampleFor(itemType);
        if (item is null)
        {
            return null;
        }

        return $$"""
            { "items": [ {{item}} ], "pageNumber": 1, "pageSize": 10, "totalCount": 1, "totalPages": 1 }
            """;
    }
}

using System.Text.Json.Nodes;
using GestaoDePedidos.Common.Responses;
using GestaoDePedidos.Dtos.Pedidos;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GestaoDePedidos.Common.Swagger;

public class SchemaExamplesFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concreteSchema)
        {
            return;
        }

        if (context.Type == typeof(CriarPedidoRequest))
        {
            concreteSchema.Example = JsonNode.Parse("""
                {
                  "clienteId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                  "itens": [
                    { "produtoId": "9c1f0b1a-2b3c-4d5e-8f6a-1234567890ab", "quantidade": 2 },
                    { "produtoId": "2a2f0b1a-2b3c-4d5e-8f6a-abcdef123456", "quantidade": 1.5 }
                  ]
                }
                """);
        }
        else if (context.Type == typeof(ApiErrorResponse))
        {
            concreteSchema.Example = JsonNode.Parse("""
                {
                  "status": 409,
                  "title": "Conflito de dados",
                  "detail": "Não foi possível concluir o pedido devido a conflito de estoque. Tente novamente.",
                  "errors": null
                }
                """);
        }
    }
}

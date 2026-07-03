using System.Text.Json.Nodes;
using GestaoDePedidos.Controllers;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GestaoDePedidos.Common.Swagger;

/// <summary>
/// ApiErrorResponse é reaproveitado por praticamente todo endpoint em vários status codes diferentes,
/// então um exemplo a nível de schema (ver SchemaExamplesFilter) só consegue mostrar um exemplo fixo
/// pra esse tipo inteiro. Este filtro injeta, por operação e por status code, o texto real que o
/// endpoint correspondente produz (extraído literalmente das exceções lançadas em Services/*.cs).
/// </summary>
public class ErrorResponseExamplesFilter : IOperationFilter
{
    private const string ClienteNaoEncontrado = "Cliente não encontrado.";
    private const string ProdutoNaoEncontrado = "Produto não encontrado.";
    private const string PedidoNaoEncontrado = "Pedido não encontrado.";
    private const string EstoqueInsuficiente = "Estoque insuficiente para o produto informado.";

    private static readonly Dictionary<string, string> DefaultsByStatus = new()
    {
        ["401"] = Error(401, "Não autenticado", "Token de acesso ausente, inválido ou expirado."),
        ["403"] = Error(403, "Acesso negado", "Você não tem permissão para acessar este recurso."),
        ["500"] = Error(500, "Erro interno do servidor", "Ocorreu um erro inesperado.")
    };

    private static readonly Dictionary<(Type Controller, string Action), Dictionary<string, string>> ExamplesByAction = new()
    {
        [(typeof(AuthController), nameof(AuthController.Login))] = new()
        {
            ["400"] = ValidationError(("Email", "O e-mail é obrigatório.")),
            ["401"] = Error(401, "Não autenticado", "Login ou senha inválidos.")
        },
        [(typeof(ClienteController), nameof(ClienteController.Criar))] = new()
        {
            ["400"] = Error(400, "Erro de validação", "Documento inválido. Informe um CPF ou CNPJ válido."),
            ["409"] = Error(409, "Conflito de dados", "Já existe um cliente ativo com este e-mail.")
        },
        [(typeof(ClienteController), nameof(ClienteController.ObterPorId))] = new()
        {
            ["404"] = Error(404, "Recurso não encontrado", ClienteNaoEncontrado)
        },
        [(typeof(ClienteController), nameof(ClienteController.AtualizarStatus))] = new()
        {
            ["404"] = Error(404, "Recurso não encontrado", ClienteNaoEncontrado)
        },
        [(typeof(ProdutoController), nameof(ProdutoController.Criar))] = new()
        {
            ["400"] = Error(400, "Erro de validação", "O preço deve ser maior que zero.")
        },
        [(typeof(ProdutoController), nameof(ProdutoController.ObterPorId))] = new()
        {
            ["404"] = Error(404, "Recurso não encontrado", ProdutoNaoEncontrado)
        },
        [(typeof(ProdutoController), nameof(ProdutoController.Atualizar))] = new()
        {
            ["400"] = Error(400, "Erro de validação", "Não é possível desabilitar a venda fracionada: o estoque atual possui valor fracionado."),
            ["404"] = Error(404, "Recurso não encontrado", ProdutoNaoEncontrado)
        },
        [(typeof(ProdutoController), nameof(ProdutoController.AtualizarStatus))] = new()
        {
            ["404"] = Error(404, "Recurso não encontrado", ProdutoNaoEncontrado)
        },
        [(typeof(ProdutoController), nameof(ProdutoController.AtualizarEstoque))] = new()
        {
            ["400"] = Error(400, "Erro de validação", EstoqueInsuficiente),
            ["404"] = Error(404, "Recurso não encontrado", ProdutoNaoEncontrado)
        },
        [(typeof(PedidoController), nameof(PedidoController.Criar))] = new()
        {
            ["400"] = Error(400, "Erro de validação", EstoqueInsuficiente),
            ["404"] = Error(404, "Recurso não encontrado", ClienteNaoEncontrado),
            ["409"] = Error(409, "Conflito de dados", "Não foi possível concluir o pedido devido a conflito de estoque. Tente novamente.")
        },
        [(typeof(PedidoController), nameof(PedidoController.ObterPorId))] = new()
        {
            ["404"] = Error(404, "Recurso não encontrado", PedidoNaoEncontrado)
        },
        [(typeof(PedidoController), nameof(PedidoController.AtualizarStatus))] = new()
        {
            ["400"] = Error(400, "Erro de validação", "Transição de status inválida de Enviado para Criado."),
            ["404"] = Error(404, "Recurso não encontrado", PedidoNaoEncontrado),
            ["409"] = Error(409, "Conflito de dados", "O status do pedido foi alterado por outra requisição. Tente novamente.")
        }
    };

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType is null)
        {
            return;
        }

        ExamplesByAction.TryGetValue((context.MethodInfo.DeclaringType, context.MethodInfo.Name), out var perActionExamples);

        foreach (var (statusCode, response) in operation.Responses ?? [])
        {
            if (response is not OpenApiResponse concreteResponse
                || concreteResponse.Content is null
                || !concreteResponse.Content.TryGetValue("application/json", out var mediaType)
                || mediaType is null)
            {
                continue;
            }

            var json = perActionExamples?.GetValueOrDefault(statusCode) ?? DefaultsByStatus.GetValueOrDefault(statusCode);
            if (json is not null)
            {
                mediaType.Example = JsonNode.Parse(json);
            }
        }
    }

    private static string Error(int status, string title, string detail) =>
        $$"""
        { "status": {{status}}, "title": "{{title}}", "detail": "{{detail}}", "errors": null }
        """;

    private static string ValidationError(params (string Field, string Message)[] fields)
    {
        var errors = string.Join(",", fields.Select(f => $$"""{ "field": "{{f.Field}}", "messages": ["{{f.Message}}"] }"""));
        return $$"""
            { "status": 400, "title": "Erro de validação", "detail": "Um ou mais campos estão inválidos.", "errors": [{{errors}}] }
            """;
    }
}

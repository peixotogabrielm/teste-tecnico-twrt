using GestaoDePedidos.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GestaoDePedidos.Common.Responses;

public static class ApiErrorResponseFactory
{
    private const string ErroDeValidacaoTitle = "Erro de validação";

    public static (int StatusCode, ApiErrorResponse Body) FromException(Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado", exception.Message),
            BadRequestException => (StatusCodes.Status400BadRequest, ErroDeValidacaoTitle, exception.Message),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Não autenticado", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflito de dados", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor", "Ocorreu um erro inesperado.")
        };

        return (statusCode, new ApiErrorResponse
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        });
    }

    public static (int StatusCode, ApiErrorResponse Body) FromModelState(ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .Select(entry => new ApiFieldError
            {
                Field = entry.Key,
                Messages = entry.Value!.Errors
                    .Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? "Valor inválido." : error.ErrorMessage)
                    .ToList()
            })
            .ToList();

        return (StatusCodes.Status400BadRequest, new ApiErrorResponse
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ErroDeValidacaoTitle,
            Detail = "Um ou mais campos estão inválidos.",
            Errors = errors
        });
    }
}

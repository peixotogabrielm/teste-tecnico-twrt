using GestaoDePedidos.Common.Responses;
using Microsoft.AspNetCore.Diagnostics;

namespace GestaoDePedidos.Common.Exceptions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Erro não tratado: {Message}", exception.Message);

        var (statusCode, body) = ApiErrorResponseFactory.FromException(exception);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(body, cancellationToken);

        return true;
    }
}

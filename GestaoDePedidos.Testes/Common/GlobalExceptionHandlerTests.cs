using System.Text.Json;
using FluentAssertions;
using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Common.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace GestaoDePedidos.Testes.Common;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _sut = new(new Mock<ILogger<GlobalExceptionHandler>>().Object);

    [Theory]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound, "Recurso não encontrado")]
    [InlineData(typeof(BadRequestException), StatusCodes.Status400BadRequest, "Erro de validação")]
    [InlineData(typeof(UnauthorizedException), StatusCodes.Status401Unauthorized, "Não autenticado")]
    [InlineData(typeof(ConflictException), StatusCodes.Status409Conflict, "Conflito de dados")]
    public async Task TryHandleAsync_DeveEscreverApiErrorResponse_ComStatusETitleCorretosParaCadaExcecaoDeNegocio(
        Type tipoExcecao, int statusEsperado, string titleEsperado)
    {
        var mensagem = "mensagem de negócio específica";
        var excecao = (Exception)Activator.CreateInstance(tipoExcecao, mensagem)!;

        var (statusCode, body) = await ExecutarAsync(excecao);

        statusCode.Should().Be(statusEsperado);
        body.Status.Should().Be(statusEsperado);
        body.Title.Should().Be(titleEsperado);
        body.Detail.Should().Be(mensagem);
        body.Errors.Should().BeNull();
    }

    [Fact]
    public async Task TryHandleAsync_NaoDeveExporMensagemDaExcecao_QuandoExcecaoForGenerica()
    {
        var excecao = new InvalidOperationException("detalhe interno sensível de implementação");

        var (statusCode, body) = await ExecutarAsync(excecao);

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.Title.Should().Be("Erro interno do servidor");
        body.Detail.Should().Be("Ocorreu um erro inesperado.");
        body.Detail.Should().NotContain("sensível");
    }

    [Fact]
    public async Task TryHandleAsync_DeveRetornarTrue_IndicandoQueAExcecaoFoiTotalmenteTratada()
    {
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var tratada = await _sut.TryHandleAsync(httpContext, new NotFoundException("x"), CancellationToken.None);

        tratada.Should().BeTrue();
    }

    private async Task<(int StatusCode, ApiErrorResponse Body)> ExecutarAsync(Exception excecao)
    {
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await _sut.TryHandleAsync(httpContext, excecao, CancellationToken.None);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            httpContext.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        return (httpContext.Response.StatusCode, body!);
    }
}

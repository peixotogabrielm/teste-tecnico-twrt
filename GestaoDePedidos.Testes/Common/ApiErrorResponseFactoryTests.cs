using FluentAssertions;
using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Common.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GestaoDePedidos.Testes.Common;

public class ApiErrorResponseFactoryTests
{
    [Fact]
    public void FromException_DeveMapear404_QuandoNotFoundException()
    {
        var excecao = new NotFoundException("Cliente não encontrado.");

        var (statusCode, body) = ApiErrorResponseFactory.FromException(excecao);

        statusCode.Should().Be(StatusCodes.Status404NotFound);
        body.Status.Should().Be(StatusCodes.Status404NotFound);
        body.Title.Should().Be("Recurso não encontrado");
        body.Detail.Should().Be("Cliente não encontrado.");
        body.Errors.Should().BeNull();
    }

    [Fact]
    public void FromException_DeveMapear400_QuandoBadRequestException()
    {
        var excecao = new BadRequestException("Estoque insuficiente para o produto informado.");

        var (statusCode, body) = ApiErrorResponseFactory.FromException(excecao);

        statusCode.Should().Be(StatusCodes.Status400BadRequest);
        body.Title.Should().Be("Erro de validação");
        body.Detail.Should().Be("Estoque insuficiente para o produto informado.");
        body.Errors.Should().BeNull();
    }

    [Fact]
    public void FromException_DeveMapear401_QuandoUnauthorizedException()
    {
        var excecao = new UnauthorizedException("E-mail ou senha inválidos.");

        var (statusCode, body) = ApiErrorResponseFactory.FromException(excecao);

        statusCode.Should().Be(StatusCodes.Status401Unauthorized);
        body.Title.Should().Be("Não autenticado");
        body.Detail.Should().Be("E-mail ou senha inválidos.");
    }

    [Fact]
    public void FromException_DeveMapear409_QuandoConflictException()
    {
        var excecao = new ConflictException("Já existe um cliente ativo com o mesmo e-mail ou documento.");

        var (statusCode, body) = ApiErrorResponseFactory.FromException(excecao);

        statusCode.Should().Be(StatusCodes.Status409Conflict);
        body.Title.Should().Be("Conflito de dados");
        body.Detail.Should().Be("Já existe um cliente ativo com o mesmo e-mail ou documento.");
    }

    [Fact]
    public void FromException_DeveMapear500ComMensagemGenerica_QuandoExcecaoNaoMapeada()
    {
        var excecao = new InvalidOperationException("detalhe interno de implementação, string de conexão etc.");

        var (statusCode, body) = ApiErrorResponseFactory.FromException(excecao);

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.Title.Should().Be("Erro interno do servidor");
        body.Detail.Should().Be("Ocorreu um erro inesperado.");
        body.Detail.Should().NotContain("detalhe interno");
    }

    [Fact]
    public void FromModelState_DeveAgruparMensagensPorCampo()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Nome", "O nome é obrigatório.");
        modelState.AddModelError("Email", "O e-mail informado é inválido.");
        modelState.AddModelError("Email", "O e-mail é obrigatório.");

        var (statusCode, body) = ApiErrorResponseFactory.FromModelState(modelState);

        statusCode.Should().Be(StatusCodes.Status400BadRequest);
        body.Status.Should().Be(StatusCodes.Status400BadRequest);
        body.Title.Should().Be("Erro de validação");
        body.Detail.Should().Be("Um ou mais campos estão inválidos.");

        body.Errors.Should().HaveCount(2);

        var erroNome = body.Errors.Should().ContainSingle(e => e.Field == "Nome").Which;
        erroNome.Messages.Should().Equal("O nome é obrigatório.");

        var erroEmail = body.Errors.Should().ContainSingle(e => e.Field == "Email").Which;
        erroEmail.Messages.Should().Equal("O e-mail informado é inválido.", "O e-mail é obrigatório.");
    }

    [Fact]
    public void FromModelState_NaoDeveIncluirCamposSemErro()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("Nome", "O nome é obrigatório.");
        modelState.SetModelValue("Email", "cliente@example.com", "cliente@example.com");

        var (_, body) = ApiErrorResponseFactory.FromModelState(modelState);

        body.Errors.Should().ContainSingle().Which.Field.Should().Be("Nome");
    }

    [Fact]
    public void FromModelState_DeveManterChaveVazia_QuandoErroNaoTemCampoEspecifico()
    {
        var modelState = new ModelStateDictionary();
        modelState.AddModelError(string.Empty, "JSON malformado no corpo da requisição.");

        var (_, body) = ApiErrorResponseFactory.FromModelState(modelState);

        body.Errors.Should().ContainSingle().Which.Field.Should().Be(string.Empty);
    }
}

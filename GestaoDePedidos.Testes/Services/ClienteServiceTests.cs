using FluentAssertions;
using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Dtos.Clientes;
using GestaoDePedidos.Entities;
using GestaoDePedidos.Repository;
using GestaoDePedidos.Services;
using Moq;

namespace GestaoDePedidos.Testes.Services;

public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepositoryMock = new();
    private readonly ClienteService _sut;

    public ClienteServiceTests()
    {
        _sut = new ClienteService(_clienteRepositoryMock.Object);
    }

    private static CreateClienteRequest CriarRequestValido() => new()
    {
        Nome = "Maria Oliveira",
        Email = "maria.oliveira@example.com",
        Documento = "52998224725" // CPF válido
    };

    [Fact]
    public async Task CriarAsync_DeveCriarCliente_QuandoDadosValidos()
    {
        // Arrange
        var request = CriarRequestValido();
        _clienteRepositoryMock.Setup(r => r.ExistsAtivoComEmailAsync(request.Email)).ReturnsAsync(false);
        _clienteRepositoryMock.Setup(r => r.ExistsAtivoComDocumentoAsync(request.Documento)).ReturnsAsync(false);

        Cliente? clienteAdicionado = null;
        _clienteRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Cliente>()))
            .Callback<Cliente>(c => clienteAdicionado = c)
            .Returns(Task.CompletedTask);

        // Act
        var response = await _sut.CriarAsync(request);

        // Assert
        response.Nome.Should().Be(request.Nome);
        response.Email.Should().Be(request.Email);
        response.Documento.Should().Be(request.Documento);
        response.Ativo.Should().BeTrue();

        clienteAdicionado.Should().NotBeNull();
        clienteAdicionado!.Ativo.Should().BeTrue();

        _clienteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Once);
        _clienteRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarConflictException_QuandoEmailJaExisteAtivo()
    {
        // Arrange
        var request = CriarRequestValido();
        _clienteRepositoryMock.Setup(r => r.ExistsAtivoComEmailAsync(request.Email)).ReturnsAsync(true);

        // Act
        var act = () => _sut.CriarAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        _clienteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarConflictException_QuandoDocumentoJaExisteAtivo()
    {
        // Arrange
        var request = CriarRequestValido();
        _clienteRepositoryMock.Setup(r => r.ExistsAtivoComEmailAsync(request.Email)).ReturnsAsync(false);
        _clienteRepositoryMock.Setup(r => r.ExistsAtivoComDocumentoAsync(request.Documento)).ReturnsAsync(true);

        // Act
        var act = () => _sut.CriarAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();
        _clienteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Theory]
    [InlineData("11111111111")] // todos os dígitos iguais
    [InlineData("12345678900")] // dígitos verificadores incorretos
    [InlineData("123")] // tamanho inválido
    public async Task CriarAsync_DeveLancarValidationException_QuandoDocumentoInvalido(string documentoInvalido)
    {
        // Arrange
        var request = CriarRequestValido();
        request.Documento = documentoInvalido;

        // Act
        var act = () => _sut.CriarAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _clienteRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarCliente_QuandoExiste()
    {
        // Arrange
        var cliente = new Cliente { Nome = "João Silva", Email = "joao@example.com", Documento = "52998224725", Ativo = true };
        _clienteRepositoryMock.Setup(r => r.GetByIdAsync(cliente.Id)).ReturnsAsync(cliente);

        // Act
        var response = await _sut.ObterPorIdAsync(cliente.Id);

        // Assert
        response.Id.Should().Be(cliente.Id);
        response.Nome.Should().Be(cliente.Nome);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveLancarNotFoundException_QuandoClienteNaoExiste()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        _clienteRepositoryMock.Setup(r => r.GetByIdAsync(idInexistente)).ReturnsAsync((Cliente?)null);

        // Act
        var act = () => _sut.ObterPorIdAsync(idInexistente);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveLancarNotFoundException_QuandoClienteNaoExiste()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        _clienteRepositoryMock.Setup(r => r.GetByIdAsync(idInexistente)).ReturnsAsync((Cliente?)null);

        // Act
        var act = () => _sut.AtualizarStatusAsync(idInexistente, false);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

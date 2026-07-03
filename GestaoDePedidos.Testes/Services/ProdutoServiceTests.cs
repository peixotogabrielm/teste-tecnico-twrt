using FluentAssertions;
using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Dtos.Produtos;
using GestaoDePedidos.Entities;
using GestaoDePedidos.Repository;
using GestaoDePedidos.Services;
using Moq;

namespace GestaoDePedidos.Testes.Services;

public class ProdutoServiceTests
{
    private readonly Mock<IRepository<Produto>> _produtoRepositoryMock = new();
    private readonly ProdutoService _sut;

    public ProdutoServiceTests()
    {
        _sut = new ProdutoService(_produtoRepositoryMock.Object);
    }

    private static CreateProdutoRequest CriarRequestValido() => new()
    {
        Nome = "Parafuso Sextavado",
        Descricao = "Parafuso sextavado M8",
        Preco = 1.50m,
        EstoqueDisponivel = 100,
        UnidadeMedida = "UN",
        PermiteVendaFracionada = false
    };

    [Fact]
    public async Task CriarAsync_DeveCriarProduto_QuandoDadosValidos()
    {
        // Arrange
        var request = CriarRequestValido();

        Produto? produtoAdicionado = null;
        _produtoRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Produto>()))
            .Callback<Produto>(p => produtoAdicionado = p)
            .Returns(Task.CompletedTask);

        // Act
        var response = await _sut.CriarAsync(request);

        // Assert
        response.Nome.Should().Be(request.Nome);
        response.Preco.Should().Be(request.Preco);
        response.EstoqueDisponivel.Should().Be(request.EstoqueDisponivel);
        response.Ativo.Should().BeTrue();

        produtoAdicionado.Should().NotBeNull();
        _produtoRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Produto>()), Times.Once);
        _produtoRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task CriarAsync_DeveLancarValidationException_QuandoPrecoInvalido(decimal precoInvalido)
    {
        // Arrange
        var request = CriarRequestValido();
        request.Preco = precoInvalido;

        // Act
        var act = () => _sut.CriarAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _produtoRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Produto>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarValidationException_QuandoEstoqueNegativo()
    {
        // Arrange
        var request = CriarRequestValido();
        request.EstoqueDisponivel = -1;

        // Act
        var act = () => _sut.CriarAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _produtoRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Produto>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarValidationException_QuandoEstoqueFracionadoSemPermissao()
    {
        // Arrange: produto não permite venda fracionada, mas o estoque informado tem casas decimais
        var request = CriarRequestValido();
        request.PermiteVendaFracionada = false;
        request.EstoqueDisponivel = 10.5m;

        // Act
        var act = () => _sut.CriarAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _produtoRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Produto>()), Times.Never);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveRetornarProduto_QuandoExiste()
    {
        // Arrange
        var produto = new Produto { Nome = "Parafuso", Preco = 1m, EstoqueDisponivel = 10, UnidadeMedida = "UN", Ativo = true };
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync(produto.Id)).ReturnsAsync(produto);

        // Act
        var response = await _sut.ObterPorIdAsync(produto.Id);

        // Assert
        response.Id.Should().Be(produto.Id);
        response.Nome.Should().Be(produto.Nome);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveLancarNotFoundException_QuandoProdutoNaoExiste()
    {
        // Arrange
        var idInexistente = Guid.NewGuid();
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync(idInexistente)).ReturnsAsync((Produto?)null);

        // Act
        var act = () => _sut.ObterPorIdAsync(idInexistente);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AtualizarAsync_DeveAtualizarDadosDoProduto_QuandoValido()
    {
        // Arrange
        var produto = new Produto { Nome = "Antigo", Preco = 1m, EstoqueDisponivel = 10, UnidadeMedida = "UN", Ativo = true };
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync(produto.Id)).ReturnsAsync(produto);

        var request = new UpdateProdutoRequest
        {
            Nome = "Novo Nome",
            Descricao = "Nova descrição",
            Preco = 9.90m,
            UnidadeMedida = "UN",
            PermiteVendaFracionada = false
        };

        // Act
        await _sut.AtualizarAsync(produto.Id, request);

        // Assert
        produto.Nome.Should().Be(request.Nome);
        produto.Preco.Should().Be(request.Preco);
        _produtoRepositoryMock.Verify(r => r.Update(produto), Times.Once);
        _produtoRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarValidationException_QuandoPrecoInvalido()
    {
        // Arrange
        var produto = new Produto { Nome = "Antigo", Preco = 1m, EstoqueDisponivel = 10, UnidadeMedida = "UN", Ativo = true };
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync(produto.Id)).ReturnsAsync(produto);

        var request = new UpdateProdutoRequest
        {
            Nome = "Novo Nome",
            Preco = 0,
            UnidadeMedida = "UN",
            PermiteVendaFracionada = false
        };

        // Act
        var act = () => _sut.AtualizarAsync(produto.Id, request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _produtoRepositoryMock.Verify(r => r.Update(It.IsAny<Produto>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarAsync_DeveLancarValidationException_QuandoDesabilitaVendaFracionadaComEstoqueFracionado()
    {
        // Arrange: produto tem estoque fracionado (2.5) e a requisição tenta desabilitar a venda fracionada
        var produto = new Produto { Nome = "Antigo", Preco = 1m, EstoqueDisponivel = 2.5m, UnidadeMedida = "KG", Ativo = true, PermiteVendaFracionada = true };
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync(produto.Id)).ReturnsAsync(produto);

        var request = new UpdateProdutoRequest
        {
            Nome = "Antigo",
            Preco = 1m,
            UnidadeMedida = "KG",
            PermiteVendaFracionada = false
        };

        // Act
        var act = () => _sut.AtualizarAsync(produto.Id, request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _produtoRepositoryMock.Verify(r => r.Update(It.IsAny<Produto>()), Times.Never);
    }

    [Fact]
    public async Task AtualizarEstoqueAsync_DeveLancarValidationException_QuandoEstoqueNegativo()
    {
        // Arrange
        var produto = new Produto { Nome = "Antigo", Preco = 1m, EstoqueDisponivel = 10, UnidadeMedida = "UN", Ativo = true };
        _produtoRepositoryMock.Setup(r => r.GetByIdAsync(produto.Id)).ReturnsAsync(produto);

        // Act
        var act = () => _sut.AtualizarEstoqueAsync(produto.Id, -5);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _produtoRepositoryMock.Verify(r => r.Update(It.IsAny<Produto>()), Times.Never);
    }
}

using FluentAssertions;
using GestaoDePedidos.Common.Exceptions;
using GestaoDePedidos.Data;
using GestaoDePedidos.Dtos.Pedidos;
using GestaoDePedidos.Entities;
using GestaoDePedidos.Enums;
using GestaoDePedidos.Services;
using GestaoDePedidos.Testes.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.Testes.Services;

/// <summary>
/// PedidoService acessa ApplicationDbContext diretamente (ADR-0015) e usa ExecuteUpdateAsync +
/// transação real (ADR-0014), recursos que o provider InMemory do EF Core não suporta.
/// Por isso estes testes rodam sobre SQLite in-memory (banco relacional de verdade), não InMemory.
/// Cada teste cria seus próprios dados via um DbContext "de seed" e usa outro DbContext para o Act,
/// e um terceiro para o Assert, simulando o ciclo de vida real de um DbContext por requisição.
/// </summary>
public class PedidoServiceTests : IDisposable
{
    private readonly SqliteContextFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private static Cliente NovoCliente(bool ativo = true) => new()
    {
        Nome = "Cliente Teste",
        Email = $"{Guid.NewGuid():N}@example.com",
        Documento = "52998224725",
        Ativo = ativo
    };

    private static Produto NovoProduto(decimal preco = 10m, decimal estoque = 100, bool ativo = true, bool permiteVendaFracionada = false) => new()
    {
        Nome = "Produto Teste",
        Preco = preco,
        EstoqueDisponivel = estoque,
        UnidadeMedida = "UN",
        Ativo = ativo,
        PermiteVendaFracionada = permiteVendaFracionada
    };

    private async Task<Guid> SeedPedidoAsync(PedidoStatus status, Guid produtoId, decimal quantidade, decimal precoUnitario)
    {
        using var context = _factory.CreateContext();
        var cliente = NovoCliente();
        var pedido = new Pedido
        {
            ClienteId = cliente.Id,
            Status = status,
            ValorTotal = quantidade * precoUnitario
        };
        pedido.Itens.Add(new PedidoItem
        {
            ProdutoId = produtoId,
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            ValorTotal = quantidade * precoUnitario
        });
        pedido.HistoricoStatus.Add(new PedidoStatusHistorico { PedidoId = pedido.Id, StatusAnterior = null, NovoStatus = status });

        context.Clientes.Add(cliente);
        context.Pedidos.Add(pedido);
        await context.SaveChangesAsync();

        return pedido.Id;
    }

    // ---------- Criação de pedido / baixa de estoque ----------

    [Fact]
    public async Task CriarAsync_DeveCriarPedidoEBaixarEstoque_QuandoClienteEProdutoValidosComEstoqueSuficiente()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var cliente = NovoCliente();
        var produto = NovoProduto(preco: 10m, estoque: 50);
        seedContext.Clientes.Add(cliente);
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();

        var request = new CriarPedidoRequest
        {
            ClienteId = cliente.Id,
            Itens = [new CriarPedidoItemRequest { ProdutoId = produto.Id, Quantidade = 3 }]
        };

        // Act
        PedidoResponse response;
        using (var actContext = _factory.CreateContext())
        {
            var sut = new PedidoService(actContext);
            response = await sut.CriarAsync(request);
        }

        // Assert
        response.Status.Should().Be(PedidoStatus.Criado);
        response.ValorTotal.Should().Be(30m);
        response.Itens.Should().ContainSingle(i => i.ProdutoId == produto.Id && i.Quantidade == 3m && i.PrecoUnitario == 10m);

        using var assertContext = _factory.CreateContext();
        var produtoFinal = await assertContext.Produtos.AsNoTracking().FirstAsync(p => p.Id == produto.Id);
        produtoFinal.EstoqueDisponivel.Should().Be(47m);

        var historico = await assertContext.PedidoStatusHistoricos.AsNoTracking().Where(h => h.PedidoId == response.Id).ToListAsync();
        historico.Should().ContainSingle(h => h.NovoStatus == PedidoStatus.Criado && h.StatusAnterior == null);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarValidationException_QuandoClienteInativo()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var cliente = NovoCliente(ativo: false);
        var produto = NovoProduto();
        seedContext.Clientes.Add(cliente);
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();

        var request = new CriarPedidoRequest
        {
            ClienteId = cliente.Id,
            Itens = [new CriarPedidoItemRequest { ProdutoId = produto.Id, Quantidade = 1 }]
        };

        using var actContext = _factory.CreateContext();
        var sut = new PedidoService(actContext);

        // Act
        var act = () => sut.CriarAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CriarAsync_DeveLancarValidationException_QuandoProdutoInativo()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var cliente = NovoCliente();
        var produto = NovoProduto(ativo: false);
        seedContext.Clientes.Add(cliente);
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();

        var request = new CriarPedidoRequest
        {
            ClienteId = cliente.Id,
            Itens = [new CriarPedidoItemRequest { ProdutoId = produto.Id, Quantidade = 1 }]
        };

        using var actContext = _factory.CreateContext();
        var sut = new PedidoService(actContext);

        // Act
        var act = () => sut.CriarAsync(request);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task CriarAsync_DeveLancarValidationExceptionENaoAlterarEstoque_QuandoEstoqueInsuficiente()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var cliente = NovoCliente();
        var produto = NovoProduto(estoque: 2);
        seedContext.Clientes.Add(cliente);
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();

        var request = new CriarPedidoRequest
        {
            ClienteId = cliente.Id,
            Itens = [new CriarPedidoItemRequest { ProdutoId = produto.Id, Quantidade = 3 }]
        };

        using (var actContext = _factory.CreateContext())
        {
            var sut = new PedidoService(actContext);

            // Act
            var act = () => sut.CriarAsync(request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }

        using var assertContext = _factory.CreateContext();
        var produtoFinal = await assertContext.Produtos.AsNoTracking().FirstAsync(p => p.Id == produto.Id);
        produtoFinal.EstoqueDisponivel.Should().Be(2m);
    }

    [Fact]
    public async Task CriarAsync_NaoDevePersistirPedidoNemAlterarEstoque_QuandoUmItemDoPedidoForInvalido()
    {
        // Arrange: item 1 é válido, item 2 pede mais do que o estoque disponível.
        // Como PedidoService valida todos os itens antes de baixar qualquer estoque,
        // a falha no item 2 garante que nada é persistido nem baixado - nem o item 1, que era válido.
        using var seedContext = _factory.CreateContext();
        var cliente = NovoCliente();
        var produtoValido = NovoProduto(preco: 10m, estoque: 50);
        var produtoSemEstoque = NovoProduto(preco: 5m, estoque: 1);
        seedContext.Clientes.Add(cliente);
        seedContext.Produtos.AddRange(produtoValido, produtoSemEstoque);
        await seedContext.SaveChangesAsync();

        var request = new CriarPedidoRequest
        {
            ClienteId = cliente.Id,
            Itens =
            [
                new CriarPedidoItemRequest { ProdutoId = produtoValido.Id, Quantidade = 5 },
                new CriarPedidoItemRequest { ProdutoId = produtoSemEstoque.Id, Quantidade = 10 }
            ]
        };

        using (var actContext = _factory.CreateContext())
        {
            var sut = new PedidoService(actContext);

            // Act
            var act = () => sut.CriarAsync(request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }

        using var assertContext = _factory.CreateContext();
        (await assertContext.Pedidos.CountAsync(p => p.ClienteId == cliente.Id)).Should().Be(0);

        var produtoValidoFinal = await assertContext.Produtos.AsNoTracking().FirstAsync(p => p.Id == produtoValido.Id);
        produtoValidoFinal.EstoqueDisponivel.Should().Be(50m);
    }

    [Fact]
    public async Task CriarAsync_DeveManterPrecoUnitarioDoItem_QuandoPrecoDoProdutoForAlteradoAposACriacaoDoPedido()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var cliente = NovoCliente();
        var produto = NovoProduto(preco: 10m, estoque: 50);
        seedContext.Clientes.Add(cliente);
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();

        var request = new CriarPedidoRequest
        {
            ClienteId = cliente.Id,
            Itens = [new CriarPedidoItemRequest { ProdutoId = produto.Id, Quantidade = 2 }]
        };

        Guid pedidoId;
        using (var actContext = _factory.CreateContext())
        {
            var sut = new PedidoService(actContext);
            var response = await sut.CriarAsync(request);
            pedidoId = response.Id;
        }

        // Act: preço do produto muda depois que o pedido já existe
        using (var updateContext = _factory.CreateContext())
        {
            var produtoParaAtualizar = await updateContext.Produtos.FirstAsync(p => p.Id == produto.Id);
            produtoParaAtualizar.Preco = 25m;
            await updateContext.SaveChangesAsync();
        }

        // Assert
        using var assertContext = _factory.CreateContext();
        var pedido = await assertContext.Pedidos.Include(p => p.Itens).AsNoTracking().FirstAsync(p => p.Id == pedidoId);
        pedido.Itens.Single().PrecoUnitario.Should().Be(10m);
        pedido.ValorTotal.Should().Be(20m);
    }

    [Fact]
    public async Task ObterPorIdAsync_DeveLancarNotFoundException_QuandoPedidoNaoExiste()
    {
        using var context = _factory.CreateContext();
        var sut = new PedidoService(context);

        var act = () => sut.ObterPorIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- Transições de status ----------

    public static IEnumerable<object[]> TransicoesValidas =>
    [
        [PedidoStatus.Criado, PedidoStatus.Pago],
        [PedidoStatus.Criado, PedidoStatus.Cancelado],
        [PedidoStatus.Pago, PedidoStatus.Enviado]
    ];

    public static IEnumerable<object[]> TransicoesInvalidas =>
    [
        [PedidoStatus.Criado, PedidoStatus.Enviado],
        [PedidoStatus.Pago, PedidoStatus.Criado],
        [PedidoStatus.Pago, PedidoStatus.Cancelado],
        [PedidoStatus.Enviado, PedidoStatus.Pago],
        [PedidoStatus.Enviado, PedidoStatus.Cancelado],
        [PedidoStatus.Cancelado, PedidoStatus.Criado],
        [PedidoStatus.Cancelado, PedidoStatus.Pago]
    ];

    [Theory]
    [MemberData(nameof(TransicoesValidas))]
    public async Task AtualizarStatusAsync_DeveAplicarNovoStatus_QuandoTransicaoForValida(PedidoStatus statusAtual, PedidoStatus novoStatus)
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var produto = NovoProduto();
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();
        var pedidoId = await SeedPedidoAsync(statusAtual, produto.Id, quantidade: 1, precoUnitario: produto.Preco);

        using (var actContext = _factory.CreateContext())
        {
            var sut = new PedidoService(actContext);

            // Act
            await sut.AtualizarStatusAsync(pedidoId, new AtualizarPedidoStatusRequest { NovoStatus = novoStatus });
        }

        // Assert
        using var assertContext = _factory.CreateContext();
        var pedido = await assertContext.Pedidos.AsNoTracking().FirstAsync(p => p.Id == pedidoId);
        pedido.Status.Should().Be(novoStatus);
    }

    [Theory]
    [MemberData(nameof(TransicoesInvalidas))]
    public async Task AtualizarStatusAsync_DeveLancarValidationExceptionEManterStatus_QuandoTransicaoForInvalida(PedidoStatus statusAtual, PedidoStatus novoStatus)
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var produto = NovoProduto();
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();
        var pedidoId = await SeedPedidoAsync(statusAtual, produto.Id, quantidade: 1, precoUnitario: produto.Preco);

        using (var actContext = _factory.CreateContext())
        {
            var sut = new PedidoService(actContext);

            // Act
            var act = () => sut.AtualizarStatusAsync(pedidoId, new AtualizarPedidoStatusRequest { NovoStatus = novoStatus });

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }

        using var assertContext = _factory.CreateContext();
        var pedido = await assertContext.Pedidos.AsNoTracking().FirstAsync(p => p.Id == pedidoId);
        pedido.Status.Should().Be(statusAtual);
    }

    [Fact]
    public async Task AtualizarStatusAsync_DeveRestaurarEstoque_QuandoPedidoForCancelado()
    {
        // Arrange: estoque já reflete a baixa feita na criação (era 100, vendeu 5 -> 95)
        using var seedContext = _factory.CreateContext();
        var produto = NovoProduto(estoque: 95);
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();
        var pedidoId = await SeedPedidoAsync(PedidoStatus.Criado, produto.Id, quantidade: 5, precoUnitario: produto.Preco);

        using (var actContext = _factory.CreateContext())
        {
            var sut = new PedidoService(actContext);

            // Act
            await sut.AtualizarStatusAsync(pedidoId, new AtualizarPedidoStatusRequest { NovoStatus = PedidoStatus.Cancelado, Motivo = "Cliente desistiu" });
        }

        // Assert
        using var assertContext = _factory.CreateContext();
        var produtoFinal = await assertContext.Produtos.AsNoTracking().FirstAsync(p => p.Id == produto.Id);
        produtoFinal.EstoqueDisponivel.Should().Be(100m);
    }

    [Fact]
    public async Task AtualizarStatusAsync_NaoDeveAlterarEstoque_QuandoPedidoForEnviado()
    {
        // Arrange
        using var seedContext = _factory.CreateContext();
        var produto = NovoProduto(estoque: 95);
        seedContext.Produtos.Add(produto);
        await seedContext.SaveChangesAsync();
        var pedidoId = await SeedPedidoAsync(PedidoStatus.Pago, produto.Id, quantidade: 5, precoUnitario: produto.Preco);

        using (var actContext = _factory.CreateContext())
        {
            var sut = new PedidoService(actContext);

            // Act
            await sut.AtualizarStatusAsync(pedidoId, new AtualizarPedidoStatusRequest { NovoStatus = PedidoStatus.Enviado });
        }

        // Assert
        using var assertContext = _factory.CreateContext();
        var produtoFinal = await assertContext.Produtos.AsNoTracking().FirstAsync(p => p.Id == produto.Id);
        produtoFinal.EstoqueDisponivel.Should().Be(95m);
    }

    // ---------- Concorrência / baixa atômica ----------

    [Fact]
    public async Task CriarAsync_DeveGarantirApenasUmaBaixa_QuandoDuasRequisicoesConcorrentesDisputamEstoqueUnitario()
    {
        // Arrange: aqui, diferente dos demais testes desta classe, usamos SQLite em modo memória
        // com cache compartilhado (Cache=Shared) em vez de uma única conexão ":memory:" privada.
        // É necessário porque este teste precisa de duas conexões INDEPENDENTES (uma por
        // "requisição" concorrente) apontando para o mesmo banco - o cenário real que o UPDATE
        // condicional via ExecuteUpdateAsync (ADR-0014) protege. Uma conexão "keep-alive" mantém
        // o banco compartilhado vivo enquanto as duas requisições concorrentes abrem suas próprias
        // conexões/DbContexts, como aconteceria com duas requisições HTTP simultâneas via DI.
        var connectionString = $"Data Source=file:{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

        await using var keepAliveConnection = new SqliteConnection(connectionString);
        await keepAliveConnection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options;

        await using (var schemaContext = new ApplicationDbContext(options))
        {
            await schemaContext.Database.EnsureCreatedAsync();
        }

        var cliente = NovoCliente();
        var produto = NovoProduto(estoque: 1);

        await using (var seedContext = new ApplicationDbContext(options))
        {
            seedContext.Clientes.Add(cliente);
            seedContext.Produtos.Add(produto);
            await seedContext.SaveChangesAsync();
        }

        // A barreira força as duas threads a chamarem CriarAsync no mesmo instante. Sem ela, as
        // duas Tasks tendem a rodar de forma efetivamente sequencial (a primeira conclui, com
        // commit incluído, antes da segunda sequer começar), o que mascararia a corrida real que
        // este teste quer reproduzir.
        using var barreiraDeLargada = new Barrier(2);

        async Task<PedidoResponse> CriarPedidoAsync()
        {
            barreiraDeLargada.SignalAndWait();

            await using var context = new ApplicationDbContext(options);
            var service = new PedidoService(context);
            var request = new CriarPedidoRequest
            {
                ClienteId = cliente.Id,
                Itens = [new CriarPedidoItemRequest { ProdutoId = produto.Id, Quantidade = 1 }]
            };

            return await service.CriarAsync(request);
        }

        // Act
        var tarefa1 = Task.Run(CriarPedidoAsync);
        var tarefa2 = Task.Run(CriarPedidoAsync);
        var resultados = await Task.WhenAll(tarefa1.ContinueWith(EnvolverResultado), tarefa2.ContinueWith(EnvolverResultado));

        // Assert: o resultado que importa é o de negócio (nunca vender duas vezes o mesmo item
        // unitário). A exceção da requisição perdedora pode ser BadRequestException (se ela leu o
        // estoque só depois da vencedora commitar) ou ConflictException (se perdeu exatamente no
        // UPDATE condicional do ADR-0014) - as duas são interleavings legítimos de uma corrida real
        // e ambas representam a mesma garantia: nenhuma baixa além da que o estoque permite.
        resultados.Count(r => r.Sucesso).Should().Be(1, "apenas uma das duas requisições concorrentes deveria conseguir baixar o único item em estoque");
        resultados.Count(r => !r.Sucesso).Should().Be(1);
        resultados.Single(r => !r.Sucesso).Excecao.Should().Match(e => e is BadRequestException || e is ConflictException);

        await using var assertContext = new ApplicationDbContext(options);
        var produtoFinal = await assertContext.Produtos.AsNoTracking().FirstAsync(p => p.Id == produto.Id);
        produtoFinal.EstoqueDisponivel.Should().Be(0m, "o estoque não pode ficar negativo nem permitir duas vendas do mesmo item unitário");

        var totalPedidos = await assertContext.Pedidos.CountAsync(p => p.ClienteId == cliente.Id);
        totalPedidos.Should().Be(1);

        static (bool Sucesso, Exception? Excecao) EnvolverResultado(Task<PedidoResponse> tarefa) =>
            tarefa.IsFaulted ? (false, tarefa.Exception!.InnerException) : (true, null);
    }
}

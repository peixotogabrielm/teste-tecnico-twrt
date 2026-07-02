using GestaoDePedidos.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GestaoDePedidos.Testes.Helpers;

/// <summary>
/// Cria um ApplicationDbContext apoiado em uma conexão SQLite in-memory dedicada e aberta
/// durante todo o teste. Necessário para PedidoService, que usa ExecuteUpdateAsync e transações
/// reais (Database.BeginTransactionAsync) - recursos que o provider InMemory do EF Core não suporta.
/// Cada instância abre sua própria conexão ":memory:", isolando completamente os dados entre testes.
/// </summary>
public sealed class SqliteContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;

    public ApplicationDbContext Context { get; }

    public SqliteContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new ApplicationDbContext(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// Cria um novo ApplicationDbContext apontando para a mesma conexão/banco desta factory,
    /// simulando uma nova requisição HTTP que abre seu próprio DbContext via DI.
    /// Necessário para testar comportamento observável entre "requisições" (ex.: releitura após
    /// alteração, ou duas chamadas concorrentes de serviço).
    /// </summary>
    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

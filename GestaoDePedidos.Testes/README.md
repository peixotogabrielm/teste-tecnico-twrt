# Testes automatizados

Suíte de testes da API Gestão de Pedidos. O foco é verificar as regras de negócio nos Services e os componentes responsáveis pelo contrato de erro.

## Estratégia

| Escopo            | Tipo                                       | Ferramentas                             | Motivo                                                                      |
| ----------------- | ------------------------------------------ | --------------------------------------- | --------------------------------------------------------------------------- |
| `ClienteService`  | unitário                                   | xUnit, Moq, FluentAssertions            | o Service depende de uma interface de repositório e pode ser isolado        |
| `ProdutoService`  | unitário                                   | xUnit, Moq, FluentAssertions            | validações e mutações podem ser verificadas sem banco                       |
| `PedidoService`   | componente com banco relacional em memória | xUnit, EF Core SQLite, FluentAssertions | criação de pedido depende de transação, `ExecuteUpdateAsync` e concorrência |
| respostas de erro | unitário                                   | xUnit, Moq, FluentAssertions            | verifica mapeamento de exceções, `ModelState` e escrita HTTP                |

O provider `Microsoft.EntityFrameworkCore.InMemory` não foi usado no fluxo de pedido porque não oferece o comportamento relacional necessário e não suporta adequadamente as operações executadas diretamente no banco. SQLite in-memory permite testar constraints, transações e updates em um banco real, embora não substitua testes contra SQL Server.

## Estrutura

```text
GestaoDePedidos.Testes/
├── Common/
│   ├── ApiErrorResponseFactoryTests.cs
│   └── GlobalExceptionHandlerTests.cs
├── Helpers/
│   └── SqliteContextFactory.cs
├── Services/
│   ├── ClienteServiceTests.cs
│   ├── PedidoServiceTests.cs
│   └── ProdutoServiceTests.cs
└── GestaoDePedidos.Testes.csproj
```

`SqliteContextFactory` mantém uma conexão `:memory:` aberta durante o teste e permite criar mais de um `ApplicationDbContext` sobre o mesmo banco. Isso simula o ciclo de uma requisição por contexto e evita que o tracking de um contexto esconda o estado realmente persistido.

O cenário concorrente usa `Mode=Memory;Cache=Shared`, duas conexões independentes e uma conexão keep-alive. Duas tarefas disputam a única unidade disponível; exatamente uma deve criar o pedido, o estoque final deve ser zero e nunca negativo.

## Como executar

Na raiz do repositório:

```powershell
dotnet test .\GestaoDePedidos\GestaoDePedidos.slnx
```

Somente o projeto de testes:

```powershell
dotnet test .\GestaoDePedidos.Testes\GestaoDePedidos.Testes.csproj
```

Com cobertura:

```powershell
dotnet test .\GestaoDePedidos\GestaoDePedidos.slnx --collect:"XPlat Code Coverage"
```

Os arquivos Cobertura são criados em `TestResults/<id>/coverage.cobertura.xml`.

Na validação local desta documentação, a suíte concluiu com **72 testes aprovados, 0 falhas e 0 ignorados**.

## Cobertura funcional

### Clientes

- criação de cliente válido e ativo;
- rejeição de e-mail já usado por cliente ativo;
- rejeição de documento já usado por cliente ativo;
- rejeição de CPF/CNPJ inválido;
- consulta por ID existente;
- `404` para consulta e mudança de status de cliente inexistente.

### Produtos

- criação de produto válido;
- preço maior que zero;
- rejeição de preço com mais de 2 casas decimais significativas;
- aceitação de zeros decimais não significativos;
- rejeição de estoque negativo;
- regra de quantidade inteira para produto sem venda fracionada;
- consulta por ID e `404`;
- atualização cadastral e de preço;
- impedimento de desativar venda fracionada quando o saldo atual é fracionado;
- entrada e saída de estoque;
- rejeição de quantidade não positiva, fracionamento inválido e saída acima do saldo.

### Pedidos

- criação com cliente e produto ativos;
- baixa de estoque e histórico inicial na mesma operação;
- arredondamento monetário por item com `MidpointRounding.AwayFromZero`;
- rejeição de cliente inativo, produto inativo e estoque insuficiente;
- ausência de persistência e de baixa parcial quando um item é inválido;
- preservação do preço unitário após alterar o preço do produto;
- agrupamento de linhas repetidas do mesmo produto;
- validação de cada linha antes do agrupamento;
- validação de fracionamento e estoque sobre a quantidade agrupada;
- `404` ao consultar pedido inexistente;
- transições válidas: `Criado → Pago`, `Criado → Cancelado` e `Pago → Enviado`;
- transições inválidas, incluindo `Pago → Cancelado`, sem mudança de estado;
- restauração de estoque em `Criado → Cancelado`;
- ausência de alteração de estoque em `Pago → Enviado`;
- disputa concorrente da última unidade, permitindo exatamente uma venda.

### Contrato de erro

- mapeamento de `NotFoundException`, `BadRequestException`, `UnauthorizedException` e `ConflictException`;
- resposta `500` genérica sem vazamento da mensagem interna;
- agrupamento das mensagens de `ModelState` por campo;
- preservação de erros sem campo específico;
- status e corpo JSON escritos pelo `GlobalExceptionHandler`;
- confirmação de que a exceção foi totalmente tratada.

## Por que esses testes foram priorizados

O risco principal da solução está nas invariantes de pedido: não vender além do estoque, não deixar baixa parcial, preservar preço histórico, respeitar a máquina de estados e devolver estoque somente no cancelamento permitido. Por isso `PedidoService` recebe testes relacionais, inclusive de concorrência, enquanto os Services de CRUD simples usam mocks.

Os testes do contrato de erro protegem uma interface transversal consumida por todos os endpoints. Eles também garantem que um erro inesperado não exponha detalhes internos.

## Fora do escopo atual

- testes de integração HTTP com `WebApplicationFactory`;
- autenticação e autorização JWT;
- Controllers, model binding e serialização ponta a ponta;
- paginação e filtro de pedidos;
- execução contra SQL Server;
- migrations e seed do administrador;
- concorrência no ajuste manual de estoque;
- teste controlado que force falha depois da primeira baixa de um pedido com vários itens;
- limite mínimo obrigatório de cobertura.

Esses cenários devem ser adicionados como testes de integração. Para compatibilidade específica do provider, uma suíte menor contra SQL Server em container é preferível a assumir que todo comportamento do SQLite é idêntico.

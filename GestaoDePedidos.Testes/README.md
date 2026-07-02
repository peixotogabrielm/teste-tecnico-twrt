# Testes automatizados — GestaoDePedidos

Suíte de testes unitários focada em **regra de negócio nos Services**, não em Controllers (que
são finos e apenas delegam para os Services — ver `docs/adr/`). Cobre as três regras centrais do
domínio: cadastro de Cliente/Produto, e o ciclo de vida de um Pedido (criação com baixa de
estoque, transições de status, cancelamento com devolução de estoque).

## Stack

| Ferramenta | Uso |
|---|---|
| **xUnit** | framework de testes |
| **Moq** | mock de `IClienteRepository` / `IRepository<Produto>` (ClienteService e ProdutoService) |
| **FluentAssertions 7.x** | asserções (fixado em 7.x porque a partir da v8 a FluentAssertions passou a exigir licença comercial paga — v7 é a última Apache 2.0) |
| **Microsoft.EntityFrameworkCore.Sqlite** | banco relacional real em memória, usado só nos testes de `PedidoService` |

## Por que SQLite in-memory só para PedidoService?

`PedidoService` é o único Service que não segue o padrão Controller → Service → Repository do
projeto: ele recebe `ApplicationDbContext` diretamente (ADR-0015) porque precisa de controle de
transação real (`Database.BeginTransactionAsync`) e de um `UPDATE` condicional via
`ExecuteUpdateAsync` para baixar estoque de forma atômica sem `RowVersion` (ADR-0014).

O provider **InMemory do EF Core não suporta `ExecuteUpdateAsync`** (lança `NotSupportedException`)
e trata transações como no-op — ou seja, não dá pra testar baixa de estoque, rollback ou
concorrência com ele. Por isso `PedidoServiceTests` usa SQLite em modo memória
(`DataSource=:memory:`), que é um banco relacional de verdade: suporta `ExecuteUpdateAsync`,
transações e bloqueios de escrita reais.

`ClienteService` e `ProdutoService` dependem só de interfaces de repositório
(`IClienteRepository`, `IRepository<Produto>`), então são testados com mocks (Moq), sem nenhum
banco envolvido.

## Estrutura

```
testes/
  Helpers/
    SqliteContextFactory.cs   — abre uma conexão SQLite ":memory:" e cria/derruba o ApplicationDbContext
  Services/
    ClienteServiceTests.cs
    ProdutoServiceTests.cs
    PedidoServiceTests.cs
```

`SqliteContextFactory` abre **uma conexão SQLite por instância de classe de teste** (o xUnit cria
uma instância nova da classe a cada `[Fact]`/`[Theory]`, então cada teste tem seu próprio banco
isolado) e a mantém aberta até o `Dispose()`, porque um banco `:memory:` do SQLite deixa de existir
assim que a conexão fecha. `CreateContext()` cria `ApplicationDbContext`s adicionais sobre essa
mesma conexão, simulando um novo `DbContext` por requisição (como aconteceria via DI numa API
real), o que evita problemas de tracking do EF ao reler dados entre "requisições" dentro do mesmo
teste.

## Como rodar

```bash
dotnet test testes/GestaoDePedidos.Testes.csproj
```

ou, na raiz do repositório, abrindo a solução `GestaoDePedidos/GestaoDePedidos.slnx` (o projeto de
testes já foi adicionado a ela):

```bash
dotnet test GestaoDePedidos/GestaoDePedidos.slnx
```

## O que cada teste verifica

### `ClienteServiceTests`

| Teste | O que verifica |
|---|---|
| `CriarAsync_DeveCriarCliente_QuandoDadosValidos` | cliente é criado como `Ativo = true`, com os dados do request, e `AddAsync`/`SaveChangesAsync` são chamados uma vez. |
| `CriarAsync_DeveLancarConflictException_QuandoEmailJaExisteAtivo` | e-mail duplicado entre clientes ativos bloqueia a criação com `ConflictException`, e nada é persistido. |
| `CriarAsync_DeveLancarConflictException_QuandoDocumentoJaExisteAtivo` | mesma regra acima, para documento duplicado. |
| `CriarAsync_DeveLancarValidationException_QuandoDocumentoInvalido` *(Theory: dígitos repetidos, dígito verificador incorreto, tamanho inválido)* | `DocumentoValidator` bloqueia CPF/CNPJ inválido antes de checar duplicidade ou persistir. |
| `ObterPorIdAsync_DeveRetornarCliente_QuandoExiste` | busca por id feliz. |
| `ObterPorIdAsync_DeveLancarNotFoundException_QuandoClienteNaoExiste` | id inexistente lança `NotFoundException` em vez de retornar nulo. |
| `AtualizarStatusAsync_DeveLancarNotFoundException_QuandoClienteNaoExiste` | mesma regra de not-found ao tentar (in)ativar um cliente que não existe. |

> A duplicidade de e-mail/documento é sempre testada apenas para clientes **ativos** porque essa é
> a regra real do sistema (`ExistsAtivoComEmailAsync`/`ExistsAtivoComDocumentoAsync`) — um cliente
> inativo pode ter o mesmo e-mail/documento de um novo cadastro.

### `ProdutoServiceTests`

| Teste | O que verifica |
|---|---|
| `CriarAsync_DeveCriarProduto_QuandoDadosValidos` | produto é criado como `Ativo = true` com os dados do request. |
| `CriarAsync_DeveLancarValidationException_QuandoPrecoInvalido` *(Theory: 0 e -10)* | preço deve ser maior que zero. |
| `CriarAsync_DeveLancarValidationException_QuandoEstoqueNegativo` | estoque inicial não pode ser negativo. |
| `CriarAsync_DeveLancarValidationException_QuandoEstoqueFracionadoSemPermissao` | produto com `PermiteVendaFracionada = false` não aceita estoque com casas decimais (`QuantidadeValidator`). |
| `ObterPorIdAsync_DeveRetornarProduto_QuandoExiste` / `ObterPorIdAsync_DeveLancarNotFoundException_QuandoProdutoNaoExiste` | busca por id feliz e caminho not-found. |
| `AtualizarAsync_DeveAtualizarDadosDoProduto_QuandoValido` | edição de nome/descrição/preço/unidade é aplicada e persistida. |
| `AtualizarAsync_DeveLancarValidationException_QuandoPrecoInvalido` | mesma regra de preço > 0 também vale na atualização. |
| `AtualizarAsync_DeveLancarValidationException_QuandoDesabilitaVendaFracionadaComEstoqueFracionado` | não é possível desligar `PermiteVendaFracionada` se o estoque atual já tem valor fracionado (ex.: 2.5) — regra específica de `ProdutoService.AtualizarAsync`. |
| `AtualizarEstoqueAsync_DeveLancarValidationException_QuandoEstoqueNegativo` | ajuste manual de estoque também não pode ficar negativo. |

### `PedidoServiceTests`

Roda sobre SQLite in-memory (ver seção acima). Os testes de criação e transição de status usam um
`DbContext` de *seed* (para montar o cenário), um `DbContext` de *act* (onde o `PedidoService` sob
teste roda) e um `DbContext` de *assert* (para reler o estado final), replicando o ciclo de vida
real de um `DbContext` por requisição.

| Teste | O que verifica |
|---|---|
| `CriarAsync_DeveCriarPedidoEBaixarEstoque_QuandoClienteEProdutoValidosComEstoqueSuficiente` | cliente ativo + produto ativo com estoque suficiente → pedido é criado com status `Criado`, `ValorTotal` correto, histórico de status inicial gravado (`StatusAnterior = null`), **e o estoque do produto é decrementado no banco** na mesma operação. |
| `CriarAsync_DeveLancarValidationException_QuandoClienteInativo` | cliente inativo não pode ter pedido criado para ele. |
| `CriarAsync_DeveLancarValidationException_QuandoProdutoInativo` | produto inativo não pode ser vendido. |
| `CriarAsync_DeveLancarValidationExceptionENaoAlterarEstoque_QuandoEstoqueInsuficiente` | pedido pedindo mais do que o `EstoqueDisponivel` é rejeitado, **e o estoque permanece intacto** (nenhuma baixa parcial). |
| `CriarAsync_NaoDevePersistirPedidoNemAlterarEstoque_QuandoUmItemDoPedidoForInvalido` | pedido com 2 itens, um válido e outro com estoque insuficiente → **nada é persistido**: nenhum `Pedido` é salvo e o estoque do item que *seria* válido também não é alterado. Este é o teste de "rollback"/atomicidade do pedido como um todo. |
| `CriarAsync_DeveManterPrecoUnitarioDoItem_QuandoPrecoDoProdutoForAlteradoAposACriacaoDoPedido` | depois que o pedido é criado, alterar `Produto.Preco` não afeta `PedidoItem.PrecoUnitario`/`ValorTotal` nem o `ValorTotal` do pedido já existente (preço é fixado no momento da compra). |
| `ObterPorIdAsync_DeveLancarNotFoundException_QuandoPedidoNaoExiste` | busca por id inexistente. |
| `AtualizarStatusAsync_DeveAplicarNovoStatus_QuandoTransicaoForValida` *(Theory: Criado→Pago, Criado→Cancelado, Pago→Enviado, Pago→Cancelado)* | todas as transições permitidas por `PedidoStatusTransicaoValidator` são aplicadas com sucesso. |
| `AtualizarStatusAsync_DeveLancarValidationExceptionEManterStatus_QuandoTransicaoForInvalida` *(Theory: Criado→Enviado, Pago→Criado, Enviado→Pago, Enviado→Cancelado, Cancelado→Criado, Cancelado→Pago)* | qualquer transição fora da máquina de estados é rejeitada com `ValidationException`, e o status do pedido **não muda**. Inclui retrocessos (Pago→Criado) e os estados terminais (Enviado/Cancelado, que não permitem nenhuma transição). |
| `AtualizarStatusAsync_DeveRestaurarEstoque_QuandoPedidoForCancelado` | cancelar um pedido (`Criado → Cancelado`) devolve ao produto exatamente a quantidade que havia sido baixada. |
| `AtualizarStatusAsync_NaoDeveAlterarEstoque_QuandoPedidoForEnviado` | transicionar para `Enviado` (estado que não é cancelamento) **não** devolve estoque — só `Cancelado` aciona a devolução. |
| `CriarAsync_DeveGarantirApenasUmaBaixa_QuandoDuasRequisicoesConcorrentesDisputamEstoqueUnitario` | ver seção dedicada abaixo. |

#### O teste de concorrência

Prova o motivo de existir do UPDATE condicional (ADR-0014): duas "requisições" concorrentes
(`Task.Run` em threads diferentes, sincronizadas por um `Barrier` para realmente disputarem o
mesmo instante) tentam criar, cada uma, um pedido de 1 unidade do mesmo produto que só tem
`EstoqueDisponivel = 1`.

Diferente dos outros testes desta classe, aqui **não** é usada uma única conexão `:memory:`
privada — são necessárias duas conexões independentes (uma por requisição concorrente) apontando
para o mesmo banco, o que exige `Mode=Memory;Cache=Shared` com uma conexão extra "keep-alive" para
manter o banco compartilhado vivo enquanto as duas requisições abrem seus próprios `DbContext`s.

O teste verifica o resultado de negócio, não o mecanismo exato: **exatamente uma** das duas
requisições deve ter sucesso, a outra deve falhar (com `ValidationException` ou `ConflictException`
— qual das duas depende de exatamente onde a perdedora perdeu a corrida, e ambas são interleavings
legítimos), o estoque final deve ser `0` (nunca negativo, nunca com as duas baixas aplicadas), e
apenas 1 `Pedido` deve existir no banco ao final.

## Cenários pedidos e onde cada um foi coberto

| Cenário pedido | Teste |
|---|---|
| Criação de cliente válido | `ClienteServiceTests.CriarAsync_DeveCriarCliente_QuandoDadosValidos` |
| E-mail duplicado ativo | `ClienteServiceTests.CriarAsync_DeveLancarConflictException_QuandoEmailJaExisteAtivo` |
| Documento duplicado ativo | `ClienteServiceTests.CriarAsync_DeveLancarConflictException_QuandoDocumentoJaExisteAtivo` |
| Produto com preço inválido | `ProdutoServiceTests.CriarAsync_DeveLancarValidationException_QuandoPrecoInvalido` (+ `AtualizarAsync_...`) |
| Produto com estoque negativo | `ProdutoServiceTests.CriarAsync_DeveLancarValidationException_QuandoEstoqueNegativo` (+ `AtualizarEstoqueAsync_...`) |
| Pedido com cliente inativo | `PedidoServiceTests.CriarAsync_DeveLancarValidationException_QuandoClienteInativo` |
| Pedido com produto inativo | `PedidoServiceTests.CriarAsync_DeveLancarValidationException_QuandoProdutoInativo` |
| Pedido sem estoque suficiente | `PedidoServiceTests.CriarAsync_DeveLancarValidationExceptionENaoAlterarEstoque_QuandoEstoqueInsuficiente` |
| Baixa de estoque ao criar pedido | `PedidoServiceTests.CriarAsync_DeveCriarPedidoEBaixarEstoque_...` |
| Rollback quando um item do pedido é inválido | `PedidoServiceTests.CriarAsync_NaoDevePersistirPedidoNemAlterarEstoque_QuandoUmItemDoPedidoForInvalido` |
| Alteração de preço não afeta pedido antigo | `PedidoServiceTests.CriarAsync_DeveManterPrecoUnitarioDoItem_...` |
| Transições válidas de status | `PedidoServiceTests.AtualizarStatusAsync_DeveAplicarNovoStatus_QuandoTransicaoForValida` |
| Transições inválidas de status | `PedidoServiceTests.AtualizarStatusAsync_DeveLancarValidationExceptionEManterStatus_QuandoTransicaoForInvalida` |
| Retorno de estoque no cancelamento | `PedidoServiceTests.AtualizarStatusAsync_DeveRestaurarEstoque_QuandoPedidoForCancelado` |
| Pedido enviado não retorna estoque | `PedidoServiceTests.AtualizarStatusAsync_NaoDeveAlterarEstoque_QuandoPedidoForEnviado` |
| Baixa atômica via `ExecuteUpdateAsync` sob concorrência (estoque = 1) | `PedidoServiceTests.CriarAsync_DeveGarantirApenasUmaBaixa_QuandoDuasRequisicoesConcorrentesDisputamEstoqueUnitario` |

## O que ficou fora, e por quê

- **Rollback de SQL efetivo no meio de um pedido com múltiplos itens** (ex.: item 1 já teve seu
  `ExecuteUpdateAsync` executado com sucesso e o item 2 falha em seguida, forçando o
  `await using var transaction` a reverter o que o item 1 já havia gravado). No código atual, todo
  item é validado contra a leitura inicial de estoque **antes** de qualquer baixa acontecer (dois
  laços separados em `PedidoService.CriarAsync`) — então uma falha de validação nunca ocorre depois
  de uma baixa real ter sido feita dentro do mesmo pedido; só é possível chegar nesse estado via
  concorrência entre **pedidos diferentes**, que é exatamente o que
  `CriarAsync_DeveGarantirApenasUmaBaixa_...` exercita. Forçar esse interleaving específico
  *dentro de um único pedido* exigiria um seam de testabilidade na própria `PedidoService` (um
  ponto de interrupção controlável entre o loop de baixa de item 1 e item 2), o que não foi
  adicionado por não ser uma mudança mínima/indispensável no código de produção.
- **Testes de Controller.** Os Controllers só fazem *model binding* + chamada ao Service +
  tradução de exceção para status HTTP (via `GlobalExceptionHandler`), sem lógica de negócio
  própria — por isso o esforço foi todo direcionado aos Services, como combinado.
- **Paginação (`ObterPaginadoAsync`) e login/autenticação.** Fora do escopo pedido (regra de
  negócio de Cliente/Produto/Pedido); não testados aqui.

## Avisos de build conhecidos

- `NU1903` (SQLitePCLRaw.lib.e_sqlite3): vulnerabilidade reportada numa dependência transitiva do
  próprio pacote oficial `Microsoft.EntityFrameworkCore.Sqlite` 10.0.9 (a versão mais recente
  disponível no momento). É dependência só de teste, não vai para o artefato da API.

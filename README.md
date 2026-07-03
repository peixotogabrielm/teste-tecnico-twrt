# Gestão de Pedidos

API REST para cadastro de clientes e produtos, controle de estoque e ciclo de vida de pedidos. A solução usa ASP.NET Core 10, Entity Framework Core e SQL Server, com autenticação JWT para o perfil administrativo.

## Funcionalidades

- cadastro, consulta paginada e ativação/inativação de clientes;
- validação algorítmica de CPF e CNPJ;
- cadastro, consulta, atualização e ativação/inativação de produtos;
- entrada e saída manual de estoque;
- suporte a produtos com quantidade inteira ou fracionada, com até 3 casas decimais;
- criação transacional de pedidos com baixa atômica de estoque;
- preservação do preço praticado no momento da compra;
- máquina de estados e histórico de cada alteração;
- autenticação de administrador por JWT;
- contrato único de erros;
- documentação interativa via Swagger/OpenAPI;
- testes automatizados das principais regras de negócio.

## Tecnologias

| Tecnologia               | Uso                                                         |
| ------------------------ | ----------------------------------------------------------- |
| .NET 10 / ASP.NET Core   | API REST, injeção de dependência e middleware               |
| Entity Framework Core 10 | persistência, migrations, transações e updates condicionais |
| SQL Server               | banco relacional da aplicação                               |
| Swashbuckle              | documento OpenAPI e interface Swagger UI                    |
| JWT Bearer               | autenticação e autorização do administrador                 |
| BCrypt.Net-Next          | hash da senha do administrador                              |
| xUnit                    | execução dos testes                                         |
| Moq                      | isolamento dos repositórios nos testes de cliente e produto |
| FluentAssertions 7       | asserções dos testes                                        |
| SQLite in-memory         | testes relacionais e transacionais de pedido                |
| coverlet.collector       | coleta opcional de cobertura                                |

## Arquitetura

A aplicação foi mantida em um único projeto Web API, organizado por responsabilidade:

```text
HTTP
  └── Controller
        └── Service
              ├── Repository<T> ── EF Core ── SQL Server
              └── ApplicationDbContext ── EF Core ── SQL Server  (PedidoService)
```

- `Controllers` definem rotas, autorização, códigos HTTP e delegam os casos de uso.
- `Services` concentram validações e regras de negócio.
- `Repository<T>` atende o CRUD simples; `ClienteRepository` e `UsuarioRepository` adicionam consultas específicas.
- `PedidoService` usa `ApplicationDbContext` diretamente porque criação e mudança de status atravessam vários agregados e precisam de transação e `ExecuteUpdateAsync`.
- `Dtos` separam o contrato HTTP das entidades persistidas.
- `Common` reúne paginação, segurança, validações, Swagger, exceções e o contrato de erro.

Essa estrutura evita o custo de vários projetos para uma API pequena. O trade-off é uma separação apenas lógica entre camadas. Caso novos casos de uso passem a exigir transações entre agregados, a extração de uma unidade de trabalho ou de módulos de aplicação passa a ser justificável.

### Estrutura do repositório

```text
.
├── GestaoDePedidos/
│   ├── Common/
│   ├── Controllers/
│   ├── Data/
│   │   └── Configurations/
│   ├── Dtos/
│   ├── Entities/
│   ├── Enums/
│   ├── Migrations/
│   ├── Repository/
│   ├── Services/
│   ├── Program.cs
│   └── GestaoDePedidos.csproj
├── GestaoDePedidos.Testes/
│   ├── Common/
│   ├── Helpers/
│   ├── Services/
│   └── README.md
└── README.md
```

## Como executar localmente

### Pré-requisitos

- .NET SDK 10;
- SQL Server, SQL Server Express ou LocalDB;
- ferramenta `dotnet-ef` compatível com o EF Core 10.

Se necessário, instale a ferramenta:

```powershell
dotnet tool install --global dotnet-ef --version 10.*
```

### 1. Configurar o banco

A connection string padrão em `GestaoDePedidos/appsettings.json` aponta para `localhost\SQLEXPRESS`. Edite o arquivo ou sobrescreva a configuração por variável de ambiente:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost\SQLEXPRESS;Database=gestaopedidosdb;Trusted_Connection=True;TrustServerCertificate=True;"
```

### 2. Configurar JWT e administrador inicial

A aplicação falha na inicialização se estas chaves não estiverem definidas:

```powershell
$env:Jwt__SecretKey = "substitua-por-uma-chave-longa-com-pelo-menos-32-caracteres"
$env:Jwt__Issuer = "GestaoDePedidos"
$env:Jwt__Audience = "GestaoDePedidos"
$env:Jwt__ExpirationMinutes = "60"
$env:AdminCredentials__Email = "admin@exemplo.com"
$env:AdminCredentials__Password = "uma-senha-forte"
```

Na primeira inicialização, `DbSeeder` cria o administrador com hash BCrypt. O seed é idempotente por e-mail: se o usuário já existir, sua senha não é alterada.

### 3. Aplicar as migrations

As migrations já estão versionadas. A aplicação não chama `Database.Migrate()` automaticamente, portanto o banco precisa ser atualizado antes do primeiro `run`:

```powershell
dotnet ef database update --project .\GestaoDePedidos\GestaoDePedidos.csproj --startup-project .\GestaoDePedidos\GestaoDePedidos.csproj
```

### 4. Executar

```powershell
dotnet run --project .\GestaoDePedidos\GestaoDePedidos.csproj --launch-profile http
```

Com o perfil `http`, a API usa `http://localhost:5277`. O Swagger fica disponível apenas em `Development`, em:

```text
http://localhost:5277/swagger
```

### 5. Autenticar no Swagger

1. Execute `POST /api/auth/login` com o e-mail e a senha configurados no seed.
2. Copie o `accessToken`.
3. Use o botão **Authorize** do Swagger.
4. Informe o token no esquema Bearer apresentado pela interface.

## Endpoints

Todos os endpoints de clientes, produtos e pedidos exigem um JWT com role `Admin`. Apenas o login é anônimo.

| Método  | Rota                         | Resultado principal                                       |
| ------- | ---------------------------- | --------------------------------------------------------- |
| `POST`  | `/api/auth/login`            | autentica o administrador e retorna JWT                   |
| `POST`  | `/api/clientes`              | cria cliente                                              |
| `GET`   | `/api/clientes`              | lista clientes com paginação                              |
| `GET`   | `/api/clientes/{id}`         | consulta cliente                                          |
| `PATCH` | `/api/clientes/{id}/status`  | ativa ou inativa cliente                                  |
| `POST`  | `/api/produtos`              | cria produto                                              |
| `GET`   | `/api/produtos`              | lista produtos com paginação                              |
| `GET`   | `/api/produtos/{id}`         | consulta produto                                          |
| `PUT`   | `/api/produtos/{id}`         | atualiza dados e preço                                    |
| `PATCH` | `/api/produtos/{id}/status`  | ativa ou inativa produto                                  |
| `PATCH` | `/api/produtos/{id}/estoque` | registra entrada ou saída manual                          |
| `POST`  | `/api/pedidos`               | cria pedido e baixa estoque                               |
| `GET`   | `/api/pedidos`               | lista pedidos com paginação e filtro opcional por cliente |
| `GET`   | `/api/pedidos/{id}`          | consulta pedido, itens e histórico                        |
| `PATCH` | `/api/pedidos/{id}/status`   | altera status do pedido                                   |

Os schemas, exemplos de payload e respostas possíveis estão no Swagger.

## Regras de negócio e decisões

### Validação

A estratégia separa dois tipos de validação:

- Data Annotations nos DTOs tratam obrigatoriedade, tamanho, formato de e-mail e faixas numéricas durante o model binding.
- Services tratam regras que dependem do domínio ou de dados persistidos, como CPF/CNPJ, duplicidade ativa, precisão decimal, estoque, produto/cliente ativo e transição de status.

Falhas de model binding e exceções de negócio são convertidas no mesmo `ApiErrorResponse`. A escolha evita uma dependência adicional de validação e mantém as regras que precisam de banco fora dos DTOs. O custo é distribuir a validação entre atributos e Services.

CPF e CNPJ têm seus dígitos verificadores calculados; a validação não se limita à máscara. A aplicação aceita documento com ou sem pontuação para o cálculo, mas armazena o texto recebido. Normalização canônica de documento e e-mail não foi implementada.

### Persistência

O EF Core foi escolhido por oferecer mapeamento relacional, migrations, LINQ, change tracking, transações e updates executados diretamente no banco. SQL Server é o provider de produção.

As configurações Fluent API definem:

- FKs restritivas de pedido para cliente e de item para produto, preservando o histórico;
- cascata apenas de pedido para itens e histórico;
- índices filtrados únicos para e-mail e documento de clientes ativos;
- índice único de `(PedidoId, ProdutoId)`;
- enums de status e role persistidos como texto;
- `decimal(18,2)` para dinheiro e `decimal(18,3)` para quantidades.

Clientes e produtos não possuem endpoint de exclusão física. A inativação preserva as referências históricas.

### Estoque e concorrência

A criação do pedido e todas as baixas de estoque ocorrem na mesma transação. Para cada produto, a baixa é executada no banco com a condição equivalente a:

```sql
UPDATE Produtos
SET EstoqueDisponivel = EstoqueDisponivel - @quantidade
WHERE Id = @produtoId
  AND EstoqueDisponivel >= @quantidade;
```

Se duas requisições disputarem a última unidade, a condição é reavaliada pelo banco. Apenas uma baixa pode afetar a linha. A perdedora recebe:

- `400 Bad Request` se já leu o estoque após a vencedora confirmar; ou
- `409 Conflict` se o estoque mudou entre a leitura inicial e o update condicional.

Quando qualquer item falha, a transação não é confirmada: pedido, itens, histórico inicial e baixas anteriores são revertidos. O estoque não fica negativo.

O ajuste manual de estoque usa leitura, alteração em memória e `SaveChanges`, sem token de concorrência. Isso foi aceito como trade-off para uma operação administrativa de baixo volume, mas não oferece a mesma garantia da criação de pedido. Em produção, o próximo passo seria aplicar update condicional também a saídas manuais ou adicionar concorrência otimista.

Produtos repetidos no payload de um pedido são agrupados por `ProdutoId`. As quantidades são somadas e persistidas em um único item. Cada linha precisa ser positiva antes do agrupamento, e precisão e estoque são validados sobre a soma.

### Valores monetários e arredondamento

Valores monetários usam `decimal`, e não `float`/`double`, para evitar erro de representação binária. No SQL Server, preço e totais usam `decimal(18,2)`.

O preço informado:

- precisa ser maior que zero;
- pode ter no máximo 2 casas decimais significativas;
- é rejeitado, em vez de silenciosamente arredondado, quando excede essa precisão.

Ao criar um pedido:

```text
valor do item = Round(quantidade × preço unitário, 2, AwayFromZero)
valor do pedido = soma dos valores já arredondados dos itens
```

Arredondar por item garante que o total do pedido seja igual à soma exibida. `PrecoUnitario` é copiado do produto e congelado no item; alterações futuras no produto não mudam pedidos existentes.

Quantidades e estoque usam `decimal(18,3)`. Produtos fracionados aceitam até 3 casas; os demais exigem número inteiro.

### Datas e fuso horário

`DataCriacao`, `DataAtualizacao` e `DataAlteracao` são gerados com `DateTime.UtcNow` e persistidos como UTC. A API não recebe datas nos contratos atuais.

As respostas de pedido convertem `DataCriacao` e o histórico para `America/Sao_Paulo` e usam `DateTimeOffset`, preservando o offset. `ClienteResponse` e `ProdutoResponse` ainda expõem `DateTime` diretamente. Portanto, a conversão de todas as respostas para o fuso solicitado não está completa; esse ponto está registrado em “Limitações e próximos passos”.

### Status e histórico

A máquina de estados implementada é:

```text
Criado ──► Pago ──► Enviado
   │
   └─────► Cancelado
```

`Enviado` e `Cancelado` são terminais. Em especial:

- `Pago → Cancelado` é inválido;
- pedir o mesmo status atual retorna `400 Bad Request`;
- transição inválida não altera o pedido nem cria histórico;
- `Criado → Cancelado` devolve o estoque na mesma transação;
- cada pedido nasce com um histórico inicial: `StatusAnterior = null` e `NovoStatus = Criado`;
- toda transição válida grava status anterior, novo status, horário UTC e motivo opcional;
- a atualização usa `WHERE Id = @id AND Status = @statusEsperado`; disputa concorrente retorna `409 Conflict`.

O enunciado também diz que um cancelamento devolve estoque “desde que ainda não enviado”, o que poderia incluir `Pago`. Foi adotada a lista explícita de transições permitidas, mais restritiva: somente `Criado → Cancelado`. Assim, pagamento é o ponto sem retorno.

### Paginação

`GET /api/clientes`, `GET /api/produtos` e `GET /api/pedidos` usam paginação por offset:

| Parâmetro    | Padrão | Regra                                                     |
| ------------ | -----: | --------------------------------------------------------- |
| `pageNumber` |    `1` | índice baseado em 1                                       |
| `pageSize`   |   `10` | máximo de `100`; valores maiores são reduzidos para `100` |
| `clienteId`  |      — | filtro opcional, apenas em `/api/pedidos`                 |

Exemplo:

```http
GET /api/pedidos?pageNumber=2&pageSize=20&clienteId=3fa85f64-5717-4562-b3fc-2c963f66afa6
```

Resposta:

```json
{
  "items": [],
  "pageNumber": 2,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

O cliente incrementa `pageNumber` enquanto ele for menor que `totalPages`. Deve enviar `pageNumber >= 1` e `pageSize >= 1`; o contrato atual não valida explicitamente os limites inferiores.

Pedidos são ordenados por criação decrescente. Clientes e produtos ainda não têm ordenação explícita, o que pode produzir páginas instáveis quando dados são inseridos durante a navegação.

### Contrato de erro

Erros de validação de campos usam:

```json
{
  "status": 400,
  "title": "Erro de validação",
  "detail": "Um ou mais campos estão inválidos.",
  "errors": [
    {
      "field": "Nome",
      "messages": ["O nome é obrigatório."]
    }
  ]
}
```

Erros de negócio, autenticação, autorização e falhas inesperadas usam o mesmo envelope, com `errors: null`. Exceções não mapeadas retornam mensagem genérica e não expõem detalhes internos.

## Testes

A estratégia prioriza a camada de Service, onde estão as regras de negócio:

- `ClienteService` e `ProdutoService`: testes unitários com repositórios mockados;
- `PedidoService`: testes com SQLite in-memory para exercitar EF Core relacional, transações, `ExecuteUpdateAsync`, rollback e concorrência;
- contrato de erro: testes unitários de factory e exception handler.

Execute a solução inteira:

```powershell
dotnet test .\GestaoDePedidos\GestaoDePedidos.slnx
```

Para coletar cobertura:

```powershell
dotnet test .\GestaoDePedidos\GestaoDePedidos.slnx --collect:"XPlat Code Coverage"
```

O detalhamento de escopo e cenários está em [`GestaoDePedidos.Testes/README.md`](GestaoDePedidos.Testes/README.md).

## Decisões técnicas

As decisões principais estão registradas aqui para que o README seja suficiente, sem depender dos ADRs:

- **Arquitetura:** Web API em um único projeto ASP.NET Core, separado por pastas de responsabilidade.
- **Persistência:** Entity Framework Core com SQL Server e migrations versionadas.
- **Acesso a dados:** repositório genérico para CRUD e paginação; acesso direto ao `ApplicationDbContext` no fluxo transacional de pedidos.
- **Contrato HTTP:** DTOs separados das entidades e mapeamento manual por extension methods.
- **Validação:** Data Annotations para formato e obrigatoriedade; Services para regras de negócio e consultas ao banco.
- **Erros:** exceções de negócio tratadas globalmente e convertidas para `ApiErrorResponse`.
- **Segurança:** JWT com role `Admin` e senha protegida por BCrypt; `Cliente` não possui login.
- **Estoque:** pedido e baixa executados na mesma transação, com `UPDATE` condicional contra concorrência.
- **Valores monetários:** `decimal(18,2)` e arredondamento por item com `MidpointRounding.AwayFromZero`.
- **Pedidos:** máquina de estados explícita e histórico para a criação e cada transição válida.
- **Paginação:** offset por `pageNumber` e `pageSize`, limitado a 100 itens por página.
- **Datas:** persistência em UTC e conversão de pedidos e histórico para `America/Sao_Paulo`.
- **Documentação:** OpenAPI e Swagger UI com Swashbuckle.
- **Testes:** xUnit, Moq, FluentAssertions, SQLite in-memory e coverlet.

## Trade-offs

| Escolha                                          | Custo ou limitação                                                                                          |
| ------------------------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| Projeto único                                    | Simplifica a solução, mas a separação entre camadas é apenas lógica.                                        |
| Repositório genérico e acesso direto ao contexto | Evita uma unidade de trabalho prematura, mas cria dois padrões de acesso a dados.                           |
| Mapeamento manual e Data Annotations             | Reduz dependências, porém gera código repetitivo e validações distribuídas.                                 |
| Exceções e contrato de erro próprio              | Simplifica os controllers, mas usa exceções para falhas esperadas e não adota diretamente `ProblemDetails`. |
| JWT apenas para administrador                    | Atende ao escopo, mas não oferece login de cliente, refresh token ou revogação.                             |
| Baixa condicional sem `RowVersion`               | Protege o estoque, mas não cobre toda alteração concorrente de `Produto`.                                   |
| Ajuste manual de estoque simples                 | Não possui histórico de movimentação nem proteção contra lost update.                                       |
| Arredondamento por item                          | Mantém o total igual à soma exibida, mas pode diferir do arredondamento feito apenas no total final.        |
| Paginação por offset                             | É simples, mas perde eficiência em páginas altas e depende de ordenação estável.                            |
| SQLite nos testes                                | Mantém a suíte rápida, mas não substitui testes HTTP ou testes contra SQL Server real.                      |
| Conversão de fuso parcial                        | Pedidos seguem `America/Sao_Paulo`, mas clientes e produtos ainda não usam o mesmo padrão.                  |

## Fora do escopo atual

- **Datas:** padronizar também cliente e produto como `DateTimeOffset` em `America/Sao_Paulo`.
- **Paginação:** validar limites inferiores e adicionar ordenação estável a clientes e produtos; cursor seria preferível em grandes volumes.
- **Concorrência administrativa:** proteger entrada/saída manual de estoque contra lost update.
- **Testes HTTP:** adicionar testes de integração para rotas, autenticação, serialização, model binding, paginação e SQL Server real.
- **Cobertura:** definir limite mínimo no pipeline e publicar relatório.
- **Infraestrutura:** adicionar CI, Dockerfile e `docker-compose` para API e SQL Server.
- **Observabilidade:** incluir logs estruturados, correlation ID, métricas e health checks.
- **Segurança:** usar secret manager, rotação de chaves, política de senha, rate limiting e refresh/revogação de token se o produto exigir sessões longas.
- **Auditoria de estoque:** persistir movimentações manuais e automáticas com usuário, motivo, saldo anterior e saldo posterior.

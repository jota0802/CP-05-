# CRUD de Produtos com C# e ADO.NET

Aplicação de console em C# que faz o cadastro de produtos (inserir, listar, buscar por ID,
atualizar e excluir) usando **ADO.NET puro**, sem ORM, com banco **SQLite**.

## Tecnologias

- .NET 8 (C#)
- ADO.NET com `Microsoft.Data.Sqlite` (`SqliteConnection`, `SqliteCommand`, `SqliteDataReader`)
- SQLite: o banco é um arquivo, não precisa instalar servidor
- `Microsoft.Extensions.Configuration.Json` para ler o `appsettings.json`
- xUnit para os testes

## Estrutura

```
├── CrudProdutos.sln
├── database/
│   └── criar-tabela-produto.sql    # script de criação da tabela Produto
├── docs/prints/                    # prints das operações funcionando
├── src/CrudProdutos/
│   ├── appsettings.json            # connection string e caminho do arquivo de log
│   ├── Program.cs                  # lê a configuração, prepara o banco e abre o menu
│   ├── Models/Produto.cs           # classe Produto
│   ├── Data/ProdutoRepository.cs   # acesso ao banco: Inserir, Listar, BuscarPorId, Atualizar, Excluir
│   ├── Data/BancoDeDados.cs        # executa o script .sql ao iniciar
│   ├── Infra/LogDeOperacoes.cs     # grava as operações em arquivo
│   └── UI/MenuConsole.cs           # interface de console: menu, leitura e validação
└── tests/CrudProdutos.Tests/       # testes do ProdutoRepository
```

## Como configurar e executar

### Pré-requisito

- [.NET SDK 8](https://dotnet.microsoft.com/download) ou mais novo (confira com `dotnet --version`).

Não é preciso instalar banco de dados: o SQLite vem no pacote NuGet e o arquivo `produtos.db`
é criado na primeira execução.

### Pelo terminal

```bash
git clone https://github.com/jota0802/fiap-csharp-cp5-crud-adonet.git
cd fiap-csharp-cp5-crud-adonet
dotnet run --project src/CrudProdutos
```

### Pelo Visual Studio

Abra o `CrudProdutos.sln`, defina `CrudProdutos` como projeto de inicialização e pressione F5.

### Banco de dados

A connection string fica em [`src/CrudProdutos/appsettings.json`](src/CrudProdutos/appsettings.json):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=produtos.db"
  },
  "Log": {
    "Arquivo": "logs/operacoes.log"
  }
}
```

- Ao iniciar, a aplicação executa o script [`database/criar-tabela-produto.sql`](database/criar-tabela-produto.sql).
  Como ele usa `CREATE TABLE IF NOT EXISTS`, a tabela é criada na primeira vez e os dados são
  mantidos nas execuções seguintes.
- O `produtos.db` e a pasta `logs/` são criados na pasta de onde a aplicação é executada
  (na raiz do repositório, usando o comando acima).
- Para criar o banco manualmente, sem a aplicação: `sqlite3 produtos.db < database/criar-tabela-produto.sql`.

Tabela `Produto`:

| Coluna    | Tipo          | Regra                          |
|-----------|---------------|--------------------------------|
| Id        | INTEGER       | chave primária, autoincremento |
| Nome      | TEXT          | obrigatório                    |
| Preco     | DECIMAL(10,2) | obrigatório, maior ou igual a 0 |
| Estoque   | INTEGER       | obrigatório, maior ou igual a 0 |
| Categoria | TEXT          | obrigatório                    |

### Testes

```bash
dotnet test
```

São 9 testes do `ProdutoRepository`. Cada um cria um banco SQLite temporário com o mesmo script
`.sql` da aplicação e cobre inserir, listar, buscar, atualizar, excluir, o caso de ID inexistente,
um texto com SQL injection gravado como dado comum e a regra `CHECK` do banco rejeitando preço negativo.

## Menu

```
1 - Inserir produto
2 - Listar produtos
3 - Buscar produto por ID
4 - Atualizar produto
5 - Excluir produto
0 - Sair
```

No **Atualizar**, apertar Enter mantém o valor atual do campo. O **Excluir** mostra o produto e
pede confirmação. O preço aceita vírgula ou ponto (`19,90` ou `19.90`).

## Requisitos técnicos: onde cada um está

| Requisito | Onde |
|---|---|
| SQLite | pacote `Microsoft.Data.Sqlite` no [`CrudProdutos.csproj`](src/CrudProdutos/CrudProdutos.csproj) |
| Script `.sql` da tabela | [`database/criar-tabela-produto.sql`](database/criar-tabela-produto.sql) |
| Connection string no `appsettings.json` | [`appsettings.json`](src/CrudProdutos/appsettings.json), lida no [`Program.cs`](src/CrudProdutos/Program.cs) |
| Classe `Produto` | [`Models/Produto.cs`](src/CrudProdutos/Models/Produto.cs) |
| Classe `ProdutoRepository` | [`Data/ProdutoRepository.cs`](src/CrudProdutos/Data/ProdutoRepository.cs) |
| Métodos `Inserir`, `Listar`, `BuscarPorId`, `Atualizar` e `Excluir` | `ProdutoRepository` |
| `ExecuteNonQuery` no INSERT, UPDATE e DELETE | `Inserir`, `Atualizar` e `Excluir` |
| `ExecuteReader` nos SELECT | `Listar`, `BuscarPorId` e a leitura do ID gerado no `Inserir` |
| Mapeamento manual do DataReader para `Produto` | método `Mapear` do `ProdutoRepository` |
| SQL parametrizado | todos os comandos usam parâmetros (`@Id`, `@Nome`, `@Preco`, `@Estoque`, `@Categoria`) |
| Tratamento de exceções do banco | `MenuConsole.ExecutarOperacao` e `Program.cs` capturam `DbException` |
| Registro das operações em arquivo | [`Infra/LogDeOperacoes.cs`](src/CrudProdutos/Infra/LogDeOperacoes.cs), gravando em `logs/operacoes.log` |
| Separação entre interface e Repository | [`UI/MenuConsole.cs`](src/CrudProdutos/UI/MenuConsole.cs) não tem SQL; o `ProdutoRepository` não usa o console |

## Log de operações

Cada operação vira uma linha em `logs/operacoes.log`:

```
2026-09-29 19:30:59 | INICIO    | Aplicação iniciada.
2026-09-29 19:30:59 | INSERIR   | Id=1; Nome=Teclado mecânico; Preco=349.90; Estoque=10; Categoria=Periféricos
2026-09-29 19:30:59 | LISTAR    | 3 produto(s) retornado(s).
2026-09-29 19:30:59 | BUSCAR    | Id=2 encontrado.
2026-09-29 19:30:59 | ATUALIZAR | Id=1; Nome=Teclado mecânico; Preco=299.90; Estoque=10; Categoria=Periféricos
2026-09-29 19:30:59 | EXCLUIR   | Id=3; Nome=Monitor 24"; Preco=1099.00; Estoque=5; Categoria=Monitores
2026-09-29 19:30:59 | BUSCAR    | Id=99 não encontrado.
2026-09-29 19:30:59 | FIM       | Aplicação encerrada.
```

Erros de banco também são registrados, com a linha `ERRO` e a mensagem da exceção.

## Prints

<!-- PRINTS -->

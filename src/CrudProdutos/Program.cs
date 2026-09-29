using System.Data.Common;
using System.Text;
using CrudProdutos.Data;
using CrudProdutos.Infra;
using CrudProdutos.UI;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = Encoding.UTF8;

// 1. Configuração: a connection string fica no appsettings.json.
string connectionString;
string arquivoDeLog;
try
{
    var configuracao = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

    connectionString = configuracao.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não encontrada.");
    arquivoDeLog = configuracao["Log:Arquivo"] ?? "logs/operacoes.log";
}
catch (Exception ex) when (ex is IOException or InvalidDataException or FormatException or InvalidOperationException)
{
    Console.WriteLine($"Erro ao ler o appsettings.json: {ex.Message}");
    return 1;
}

var log = new LogDeOperacoes(arquivoDeLog);

// 2. Banco: executa o script database/criar-tabela-produto.sql (cria a tabela se ela não existir).
try
{
    var script = Path.Combine(AppContext.BaseDirectory, "database", "criar-tabela-produto.sql");
    BancoDeDados.CriarTabela(connectionString, script);
}
catch (Exception ex) when (ex is DbException or IOException or ArgumentException)
{
    log.Registrar("ERRO", $"Falha ao preparar o banco de dados: {ex.Message}");
    Console.WriteLine("Não foi possível preparar o banco de dados.");
    Console.WriteLine($"Detalhe: {ex.Message}");
    return 1;
}

// 3. Interface: o menu usa o repositório para todas as operações.
try
{
    var menu = new MenuConsole(new ProdutoRepository(connectionString), log);
    menu.Executar();
    return 0;
}
catch (Exception ex)
{
    log.Registrar("ERRO", $"Erro inesperado: {ex}");
    Console.WriteLine($"Erro inesperado: {ex.Message}");
    return 1;
}

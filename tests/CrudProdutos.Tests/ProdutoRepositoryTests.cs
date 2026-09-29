using System.Data.Common;
using CrudProdutos.Data;
using CrudProdutos.Models;
using Xunit;

namespace CrudProdutos.Tests;

/// <summary>
/// Cada teste usa um arquivo SQLite novo e temporário, criado com o mesmo
/// script .sql da aplicação, e apagado no final.
/// </summary>
public class ProdutoRepositoryTests : IDisposable
{
    private readonly string _arquivoDoBanco;
    private readonly ProdutoRepository _repositorio;

    public ProdutoRepositoryTests()
    {
        _arquivoDoBanco = Path.Combine(Path.GetTempPath(), $"produtos-teste-{Guid.NewGuid():N}.db");
        // Pooling=False fecha o arquivo de verdade ao fim de cada operação (no Windows, senão, ele fica travado).
        var connectionString = $"Data Source={_arquivoDoBanco};Pooling=False";

        var script = Path.Combine(AppContext.BaseDirectory, "database", "criar-tabela-produto.sql");
        BancoDeDados.CriarTabela(connectionString, script);
        _repositorio = new ProdutoRepository(connectionString);
    }

    public void Dispose()
    {
        File.Delete(_arquivoDoBanco);
    }

    private static Produto NovoProduto(string nome = "Teclado mecânico", decimal preco = 349.90m) => new()
    {
        Nome = nome,
        Preco = preco,
        Estoque = 10,
        Categoria = "Periféricos"
    };

    [Fact]
    public void Inserir_GeraIdEGravaTodosOsCampos()
    {
        var id = _repositorio.Inserir(NovoProduto());

        var salvo = _repositorio.BuscarPorId(id);

        Assert.True(id > 0);
        Assert.NotNull(salvo);
        Assert.Equal("Teclado mecânico", salvo.Nome);
        Assert.Equal(349.90m, salvo.Preco);
        Assert.Equal(10, salvo.Estoque);
        Assert.Equal("Periféricos", salvo.Categoria);
    }

    [Fact]
    public void Listar_DevolveTodosOsProdutosEmOrdemDeId()
    {
        var primeiro = _repositorio.Inserir(NovoProduto("Mouse"));
        var segundo = _repositorio.Inserir(NovoProduto("Monitor"));

        var produtos = _repositorio.Listar();

        Assert.Equal(new[] { primeiro, segundo }, produtos.Select(p => p.Id));
        Assert.Equal(new[] { "Mouse", "Monitor" }, produtos.Select(p => p.Nome));
    }

    [Fact]
    public void BuscarPorId_DevolveNullQuandoNaoExiste()
    {
        Assert.Null(_repositorio.BuscarPorId(999));
    }

    [Fact]
    public void Atualizar_AlteraOsCamposDoProduto()
    {
        var produto = NovoProduto();
        _repositorio.Inserir(produto);

        produto.Nome = "Teclado sem fio";
        produto.Preco = 299.00m;
        produto.Estoque = 3;
        produto.Categoria = "Acessórios";
        var atualizou = _repositorio.Atualizar(produto);

        var salvo = _repositorio.BuscarPorId(produto.Id);
        Assert.True(atualizou);
        Assert.NotNull(salvo);
        Assert.Equal("Teclado sem fio", salvo.Nome);
        Assert.Equal(299.00m, salvo.Preco);
        Assert.Equal(3, salvo.Estoque);
        Assert.Equal("Acessórios", salvo.Categoria);
    }

    [Fact]
    public void Atualizar_DevolveFalseQuandoNaoExiste()
    {
        var inexistente = NovoProduto();
        inexistente.Id = 999;

        Assert.False(_repositorio.Atualizar(inexistente));
    }

    [Fact]
    public void Excluir_RemoveOProduto()
    {
        var id = _repositorio.Inserir(NovoProduto());

        var excluiu = _repositorio.Excluir(id);

        Assert.True(excluiu);
        Assert.Null(_repositorio.BuscarPorId(id));
        Assert.Empty(_repositorio.Listar());
    }

    [Fact]
    public void Excluir_DevolveFalseQuandoNaoExiste()
    {
        Assert.False(_repositorio.Excluir(999));
    }

    [Fact]
    public void ParametrosSql_GravamTextoMaliciosoComoDadoComum()
    {
        // Se o SQL fosse montado concatenando texto, isto apagaria a tabela.
        const string nomeMalicioso = "x'); DROP TABLE Produto; --";

        var id = _repositorio.Inserir(NovoProduto(nomeMalicioso));

        Assert.Equal(nomeMalicioso, _repositorio.BuscarPorId(id)?.Nome);
        Assert.Single(_repositorio.Listar());
    }

    [Fact]
    public void Banco_RejeitaPrecoNegativoComExcecaoDeBanco()
    {
        // A regra CHECK (Preco >= 0) do script é a última barreira contra dado inválido.
        Assert.ThrowsAny<DbException>(() => _repositorio.Inserir(NovoProduto(preco: -1m)));
    }
}

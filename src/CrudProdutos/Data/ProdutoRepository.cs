using CrudProdutos.Models;
using Microsoft.Data.Sqlite;

namespace CrudProdutos.Data;

/// <summary>
/// Único ponto de acesso à tabela Produto. Usa ADO.NET puro: cada método abre a
/// própria conexão, monta um comando parametrizado e fecha tudo no fim (using).
/// Não conhece o console: quem mostra mensagem e trata erro é a interface.
/// </summary>
public class ProdutoRepository
{
    private const string ColunasSelect = "SELECT Id, Nome, Preco, Estoque, Categoria FROM Produto";

    private readonly string _connectionString;

    public ProdutoRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Insere o produto e devolve o Id gerado pelo banco.</summary>
    public int Inserir(Produto produto)
    {
        using var conexao = new SqliteConnection(_connectionString);
        conexao.Open();

        using var comando = conexao.CreateCommand();
        comando.CommandText =
            "INSERT INTO Produto (Nome, Preco, Estoque, Categoria) " +
            "VALUES (@Nome, @Preco, @Estoque, @Categoria);";
        AdicionarParametrosDosCampos(comando, produto);
        comando.ExecuteNonQuery();

        // last_insert_rowid() vale por conexão, então roda na mesma conexão do INSERT.
        using var consultaId = conexao.CreateCommand();
        consultaId.CommandText = "SELECT last_insert_rowid();";
        using var leitor = consultaId.ExecuteReader();
        leitor.Read();
        produto.Id = leitor.GetInt32(0);

        return produto.Id;
    }

    public List<Produto> Listar()
    {
        var produtos = new List<Produto>();

        using var conexao = new SqliteConnection(_connectionString);
        conexao.Open();

        using var comando = conexao.CreateCommand();
        comando.CommandText = ColunasSelect + " ORDER BY Id;";

        using var leitor = comando.ExecuteReader();
        while (leitor.Read())
        {
            produtos.Add(Mapear(leitor));
        }

        return produtos;
    }

    /// <summary>Devolve o produto com o Id informado, ou null se ele não existir.</summary>
    public Produto? BuscarPorId(int id)
    {
        using var conexao = new SqliteConnection(_connectionString);
        conexao.Open();

        using var comando = conexao.CreateCommand();
        comando.CommandText = ColunasSelect + " WHERE Id = @Id;";
        comando.Parameters.Add("@Id", SqliteType.Integer).Value = id;

        using var leitor = comando.ExecuteReader();
        return leitor.Read() ? Mapear(leitor) : null;
    }

    /// <summary>Atualiza todos os campos do produto. Devolve false se o Id não existir.</summary>
    public bool Atualizar(Produto produto)
    {
        using var conexao = new SqliteConnection(_connectionString);
        conexao.Open();

        using var comando = conexao.CreateCommand();
        comando.CommandText =
            "UPDATE Produto " +
            "SET Nome = @Nome, Preco = @Preco, Estoque = @Estoque, Categoria = @Categoria " +
            "WHERE Id = @Id;";
        AdicionarParametrosDosCampos(comando, produto);
        comando.Parameters.Add("@Id", SqliteType.Integer).Value = produto.Id;

        return comando.ExecuteNonQuery() > 0;
    }

    /// <summary>Exclui o produto. Devolve false se o Id não existir.</summary>
    public bool Excluir(int id)
    {
        using var conexao = new SqliteConnection(_connectionString);
        conexao.Open();

        using var comando = conexao.CreateCommand();
        comando.CommandText = "DELETE FROM Produto WHERE Id = @Id;";
        comando.Parameters.Add("@Id", SqliteType.Integer).Value = id;

        return comando.ExecuteNonQuery() > 0;
    }

    private static void AdicionarParametrosDosCampos(SqliteCommand comando, Produto produto)
    {
        comando.Parameters.Add("@Nome", SqliteType.Text).Value = produto.Nome;
        comando.Parameters.Add("@Preco", SqliteType.Real).Value = produto.Preco;
        comando.Parameters.Add("@Estoque", SqliteType.Integer).Value = produto.Estoque;
        comando.Parameters.Add("@Categoria", SqliteType.Text).Value = produto.Categoria;
    }

    /// <summary>Mapeamento manual de uma linha do DataReader para um objeto Produto.</summary>
    private static Produto Mapear(SqliteDataReader leitor)
    {
        return new Produto
        {
            Id = leitor.GetInt32(leitor.GetOrdinal("Id")),
            Nome = leitor.GetString(leitor.GetOrdinal("Nome")),
            Preco = leitor.GetDecimal(leitor.GetOrdinal("Preco")),
            Estoque = leitor.GetInt32(leitor.GetOrdinal("Estoque")),
            Categoria = leitor.GetString(leitor.GetOrdinal("Categoria"))
        };
    }
}

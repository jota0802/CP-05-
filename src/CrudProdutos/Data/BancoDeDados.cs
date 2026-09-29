using Microsoft.Data.Sqlite;

namespace CrudProdutos.Data;

public static class BancoDeDados
{
    /// <summary>
    /// Executa o script database/criar-tabela-produto.sql. Como ele usa
    /// CREATE TABLE IF NOT EXISTS, pode rodar a cada inicialização.
    /// </summary>
    public static void CriarTabela(string connectionString, string caminhoDoScript)
    {
        var script = File.ReadAllText(caminhoDoScript);

        using var conexao = new SqliteConnection(connectionString);
        conexao.Open();

        using var comando = conexao.CreateCommand();
        comando.CommandText = script;
        comando.ExecuteNonQuery();
    }
}

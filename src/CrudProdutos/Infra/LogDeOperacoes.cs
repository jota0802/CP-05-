namespace CrudProdutos.Infra;

/// <summary>
/// Registra em arquivo texto cada operação feita pela aplicação, uma por linha:
/// data e hora | operação | detalhes.
/// </summary>
public class LogDeOperacoes
{
    private readonly string _caminhoDoArquivo;

    public LogDeOperacoes(string caminhoDoArquivo)
    {
        _caminhoDoArquivo = caminhoDoArquivo;
    }

    public void Registrar(string operacao, string detalhes)
    {
        var linha = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {operacao,-9} | {detalhes}{Environment.NewLine}";

        try
        {
            var pasta = Path.GetDirectoryName(Path.GetFullPath(_caminhoDoArquivo));
            if (!string.IsNullOrEmpty(pasta))
            {
                Directory.CreateDirectory(pasta);
            }

            File.AppendAllText(_caminhoDoArquivo, linha);
        }
        catch (Exception ex)
        {
            // Falha no log não pode derrubar o cadastro: avisa e segue.
            Console.Error.WriteLine($"Aviso: não foi possível gravar o log em '{_caminhoDoArquivo}' ({ex.Message}).");
        }
    }
}

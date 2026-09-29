using System.Data.Common;
using System.Globalization;
using CrudProdutos.Data;
using CrudProdutos.Infra;
using CrudProdutos.Models;

namespace CrudProdutos.UI;

/// <summary>
/// Interface de console: lê o que o usuário digita, chama o ProdutoRepository e
/// mostra o resultado. Não há SQL aqui; todo acesso ao banco fica no repositório.
/// </summary>
public class MenuConsole
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");
    private const decimal PrecoMaximo = 99_999_999.99m; // limite da coluna DECIMAL(10,2)

    private readonly ProdutoRepository _repositorio;
    private readonly LogDeOperacoes _log;

    public MenuConsole(ProdutoRepository repositorio, LogDeOperacoes log)
    {
        _repositorio = repositorio;
        _log = log;
    }

    public void Executar()
    {
        _log.Registrar("INICIO", "Aplicação iniciada.");

        try
        {
            while (true)
            {
                MostrarMenu();
                var opcao = LerLinha().Trim();
                if (opcao == "0")
                {
                    break;
                }

                switch (opcao)
                {
                    case "1": ExecutarOperacao("inserir o produto", InserirProduto); break;
                    case "2": ExecutarOperacao("listar os produtos", ListarProdutos); break;
                    case "3": ExecutarOperacao("buscar o produto", BuscarProduto); break;
                    case "4": ExecutarOperacao("atualizar o produto", AtualizarProduto); break;
                    case "5": ExecutarOperacao("excluir o produto", ExcluirProduto); break;
                    default: Console.WriteLine("Opção inválida. Digite um número de 0 a 5."); break;
                }

                AguardarEnter();
            }
        }
        catch (EndOfStreamException)
        {
            // A entrada acabou (Ctrl+Z / Ctrl+D ou arquivo redirecionado): encerra normalmente.
        }

        _log.Registrar("FIM", "Aplicação encerrada.");
        Console.WriteLine();
        Console.WriteLine("Até logo!");
    }

    private void ExecutarOperacao(string descricao, Action operacao)
    {
        try
        {
            operacao();
        }
        catch (DbException ex) // SqliteException herda de DbException
        {
            _log.Registrar("ERRO", $"Falha ao {descricao}: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine($"Erro no banco de dados ao {descricao}.");
            Console.WriteLine($"Detalhe: {ex.Message}");
        }
    }

    private void InserirProduto()
    {
        Titulo("Inserir produto");

        var produto = new Produto
        {
            Nome = LerTextoObrigatorio("Nome"),
            Preco = LerPreco("Preço (ex.: 19,90)"),
            Estoque = LerInteiroNaoNegativo("Estoque"),
            Categoria = LerTextoObrigatorio("Categoria")
        };

        var id = _repositorio.Inserir(produto);

        _log.Registrar("INSERIR", $"Id={id}; {Descrever(produto)}");
        Console.WriteLine();
        Console.WriteLine($"Produto cadastrado com sucesso! ID gerado: {id}");
    }

    private void ListarProdutos()
    {
        Titulo("Produtos cadastrados");

        var produtos = _repositorio.Listar();
        _log.Registrar("LISTAR", $"{produtos.Count} produto(s) retornado(s).");

        if (produtos.Count == 0)
        {
            Console.WriteLine("Nenhum produto cadastrado.");
            return;
        }

        Console.WriteLine($"{"ID",-4} {"Nome",-26} {"Preço",14} {"Estoque",8}  Categoria");
        Console.WriteLine(new string('-', 72));
        foreach (var p in produtos)
        {
            Console.WriteLine($"{p.Id,-4} {Cortar(p.Nome, 26),-26} {FormatarPreco(p.Preco),14} {p.Estoque,8}  {p.Categoria}");
        }
        Console.WriteLine(new string('-', 72));
        Console.WriteLine($"Total: {produtos.Count} produto(s).");
    }

    private void BuscarProduto()
    {
        Titulo("Buscar produto por ID");

        var id = LerId();
        var produto = _repositorio.BuscarPorId(id);

        if (produto is null)
        {
            InformarNaoEncontrado("BUSCAR", id);
            return;
        }

        _log.Registrar("BUSCAR", $"Id={id} encontrado.");
        MostrarDetalhes(produto);
    }

    private void AtualizarProduto()
    {
        Titulo("Atualizar produto");

        var id = LerId();
        var produto = _repositorio.BuscarPorId(id);

        if (produto is null)
        {
            InformarNaoEncontrado("ATUALIZAR", id);
            return;
        }

        MostrarDetalhes(produto);
        Console.WriteLine("Digite os novos valores (Enter mantém o valor atual).");
        produto.Nome = LerTextoOpcional("Nome", produto.Nome);
        produto.Preco = LerPreco("Preço", produto.Preco);
        produto.Estoque = LerInteiroNaoNegativo("Estoque", produto.Estoque);
        produto.Categoria = LerTextoOpcional("Categoria", produto.Categoria);

        if (!_repositorio.Atualizar(produto))
        {
            InformarNaoEncontrado("ATUALIZAR", id);
            return;
        }

        _log.Registrar("ATUALIZAR", $"Id={id}; {Descrever(produto)}");
        Console.WriteLine();
        Console.WriteLine("Produto atualizado com sucesso!");
    }

    private void ExcluirProduto()
    {
        Titulo("Excluir produto");

        var id = LerId();
        var produto = _repositorio.BuscarPorId(id);

        if (produto is null)
        {
            InformarNaoEncontrado("EXCLUIR", id);
            return;
        }

        MostrarDetalhes(produto);
        if (!Confirmar($"Confirma a exclusão do produto {id}? (s/n): "))
        {
            _log.Registrar("EXCLUIR", $"Id={id} exclusão cancelada pelo usuário.");
            Console.WriteLine("Exclusão cancelada.");
            return;
        }

        if (!_repositorio.Excluir(id))
        {
            InformarNaoEncontrado("EXCLUIR", id);
            return;
        }

        _log.Registrar("EXCLUIR", $"Id={id}; {Descrever(produto)}");
        Console.WriteLine();
        Console.WriteLine("Produto excluído com sucesso!");
    }

    // ---------- Telas ----------

    private static void MostrarMenu()
    {
        LimparTela();
        Console.WriteLine("==========================================");
        Console.WriteLine("   CADASTRO DE PRODUTOS  (C# + ADO.NET)");
        Console.WriteLine("==========================================");
        Console.WriteLine("1 - Inserir produto");
        Console.WriteLine("2 - Listar produtos");
        Console.WriteLine("3 - Buscar produto por ID");
        Console.WriteLine("4 - Atualizar produto");
        Console.WriteLine("5 - Excluir produto");
        Console.WriteLine("0 - Sair");
        Console.Write("Escolha uma opção: ");
    }

    private static void Titulo(string texto)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {texto} ---");
    }

    private static void MostrarDetalhes(Produto p)
    {
        Console.WriteLine();
        Console.WriteLine($"ID:        {p.Id}");
        Console.WriteLine($"Nome:      {p.Nome}");
        Console.WriteLine($"Preço:     {FormatarPreco(p.Preco)}");
        Console.WriteLine($"Estoque:   {p.Estoque}");
        Console.WriteLine($"Categoria: {p.Categoria}");
        Console.WriteLine();
    }

    private void InformarNaoEncontrado(string operacao, int id)
    {
        _log.Registrar(operacao, $"Id={id} não encontrado.");
        Console.WriteLine();
        Console.WriteLine($"Nenhum produto encontrado com o ID {id}.");
    }

    private static void AguardarEnter()
    {
        Console.WriteLine();
        Console.Write("Pressione Enter para voltar ao menu...");
        LerLinha();
    }

    private static void LimparTela()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Console sem suporte a limpar a tela: segue sem limpar.
        }
    }

    // ---------- Leitura e validação da entrada ----------

    private static string LerLinha()
    {
        // null = fim da entrada; sem isso o menu entraria em laço infinito.
        return Console.ReadLine() ?? throw new EndOfStreamException();
    }

    private static string LerTextoObrigatorio(string rotulo)
    {
        while (true)
        {
            Console.Write($"{rotulo}: ");
            var texto = LerLinha().Trim();
            if (texto.Length > 0)
            {
                return texto;
            }
            Console.WriteLine("  Campo obrigatório. Tente novamente.");
        }
    }

    private static string LerTextoOpcional(string rotulo, string atual)
    {
        Console.Write($"{rotulo} [{atual}]: ");
        var texto = LerLinha().Trim();
        return texto.Length > 0 ? texto : atual;
    }

    private static decimal LerPreco(string rotulo, decimal? atual = null)
    {
        while (true)
        {
            Console.Write(atual is null ? $"{rotulo}: " : $"{rotulo} [{FormatarPreco(atual.Value)}]: ");
            var texto = LerLinha().Replace("R$", "").Trim();

            if (texto.Length == 0 && atual is not null)
            {
                return atual.Value;
            }

            // Scale conta as casas digitadas: "1.500" (1,500) tem 3 e é recusado em vez de virar R$ 1,50.
            if (TentarLerDecimal(texto, out var preco)
                && preco >= 0 && preco <= PrecoMaximo
                && preco.Scale <= 2)
            {
                return preco;
            }
            Console.WriteLine("  Preço inválido. Use até 2 casas decimais, ex.: 19,90 ou 1.500,00 (máximo 99.999.999,99).");
        }
    }

    private static bool TentarLerDecimal(string texto, out decimal valor)
    {
        // Com vírgula: formato brasileiro (1.234,56). Sem vírgula: o ponto é o separador decimal (19.90),
        // e um ponto seguido de 3 dígitos ("1.500") fica com 3 casas e é recusado pelo LerPreco.
        var cultura = texto.Contains(',') ? PtBr : CultureInfo.InvariantCulture;
        return decimal.TryParse(texto, NumberStyles.Number, cultura, out valor);
    }

    private static int LerInteiroNaoNegativo(string rotulo, int? atual = null)
    {
        while (true)
        {
            Console.Write(atual is null ? $"{rotulo}: " : $"{rotulo} [{atual}]: ");
            var texto = LerLinha().Trim();

            if (texto.Length == 0 && atual is not null)
            {
                return atual.Value;
            }

            if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor) && valor >= 0)
            {
                return valor;
            }
            Console.WriteLine("  Valor inválido. Digite um número inteiro maior ou igual a zero.");
        }
    }

    private static int LerId()
    {
        while (true)
        {
            Console.Write("ID do produto: ");
            var texto = LerLinha().Trim();

            if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
            {
                return id;
            }
            Console.WriteLine("  ID inválido. Digite um número inteiro maior que zero.");
        }
    }

    private static bool Confirmar(string pergunta)
    {
        while (true)
        {
            Console.Write(pergunta);
            var resposta = LerLinha().Trim().ToLowerInvariant();

            if (resposta is "s" or "sim")
            {
                return true;
            }
            if (resposta is "n" or "nao" or "não")
            {
                return false;
            }
            Console.WriteLine("  Responda s ou n.");
        }
    }

    // ---------- Formatação ----------

    private static string FormatarPreco(decimal preco) => preco.ToString("C", PtBr);

    private static string Cortar(string texto, int tamanhoMaximo)
    {
        return texto.Length <= tamanhoMaximo ? texto : texto[..(tamanhoMaximo - 3)] + "...";
    }

    private static string Descrever(Produto p)
    {
        return $"Nome={p.Nome}; Preco={p.Preco.ToString("0.00", CultureInfo.InvariantCulture)}; " +
               $"Estoque={p.Estoque}; Categoria={p.Categoria}";
    }
}

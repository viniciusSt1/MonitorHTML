using System.Security.Cryptography;  // Para calcular hash do HTML
using System.Text;
using RestSharp;    // Para enviar mensagens via UltraMsg
using HtmlAgilityPack;  // Limpar HTML
using System.Text.RegularExpressions; // Regex

class Program
{
    static async Task Main()
    {
        string url_site = Environment.GetEnvironmentVariable("MONITOR_URL");

        string ultraUrl = Environment.GetEnvironmentVariable("ULTRAMSG_URL");
        string token = Environment.GetEnvironmentVariable("ULTRAMSG_TOKEN");
        string to = Environment.GetEnvironmentVariable("ULTRAMSG_TO");

        string hashFile = "last_hash.txt";

        try
        {
            using var http = new HttpClient();
            var htmlBruto = await http.GetStringAsync(url_site);
            string html = LimparHtml(htmlBruto);

            //File.WriteAllText("pagina_baixada.html", html);

            string novoHash = CalcularHash(html);
            string antigoHash = File.Exists(hashFile) ? File.ReadAllText(hashFile) : "";

            if (antigoHash != novoHash)
            {
                Console.WriteLine("HTML mudou! Enviando alerta...");

                string msg = "Atualização detectada, verifique em: \\ " + url_site;
                await EnviarWhatsApp(ultraUrl, token, to, msg);

                File.WriteAllText(hashFile, novoHash);
            }
            else
            {
                Console.WriteLine("Nenhuma alteração detectada.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro: " + ex.Message);

            await EnviarWhatsApp(
                ultraUrl,
                token,
                to,
                "ERRO no monitoramento: " + ex.Message
            );
        }
    }

    static string LimparHtml(string html)   // Remover comentários de data de requisição etc
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Pegar apenas o <body>
        var body = doc.DocumentNode.SelectSingleNode("//body");
        if (body == null)
            return "";

        // Remover comentários
        var comentarios = body.SelectNodes("//comment()");
        if (comentarios != null)
        {
            foreach (var c in comentarios)
            {
                c.ParentNode.RemoveChild(c);
            }
        }

        // Remover scripts e estilos
        var scriptsStyles = body.SelectNodes("//script|//style");
        if (scriptsStyles != null)
        {
            foreach (var node in scriptsStyles)
            {
                node.ParentNode.RemoveChild(node);
            }
        }

        // Pegar apenas o HTML restante do body
        string htmlLimpo = body.InnerHtml;

        return htmlLimpo;
    }

    static string CalcularHash(string texto)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));
        return BitConverter.ToString(bytes);
    }

    static async Task EnviarWhatsApp(string url, string token, string to, string body)
    {
        var client = new RestClient(url);
        var request = new RestRequest("", Method.Post);

        request.AddHeader("content-type", "application/x-www-form-urlencoded");
        request.AddParameter("token", token);
        request.AddParameter("to", to);
        request.AddParameter("body", body);

        var response = await client.ExecuteAsync(request);
        Console.WriteLine("Resposta UltraMsg: " + response.Content);
    }
}

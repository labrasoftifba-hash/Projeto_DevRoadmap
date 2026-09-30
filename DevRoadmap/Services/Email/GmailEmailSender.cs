using System.Net.Mail;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Util.Store;

namespace DevRoadmap.Services.Email;

public class GmailEmailSender
{
    private const string TokenUserId = "confirmacaolabra";
    private const string SenderEmail = "confirmacaolabra@gmail.com";
    private readonly IDataStore _tokenStore;

    public GmailEmailSender()
    {
        var pastaLocal = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        var pastaTokens = Path.Combine(pastaLocal, "DevRoadmap", "Google", "tokens");
        _tokenStore = new FileDataStore(pastaTokens, fullPath: true);
    }

    public string ObterCaminhoCredenciais()
    {
        var pastaLocal = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        var caminhoCredenciais = Path.Combine(
            pastaLocal,
            "DevRoadmap",
            "Google",
            "web-client-secret.json");

        if (!File.Exists(caminhoCredenciais))
        {
            throw new FileNotFoundException(
                "O arquivo de credenciais OAuth do Google não foi encontrado no local esperado.",
                caminhoCredenciais);
        }

        return caminhoCredenciais;
    }

    public async Task EnviarEmailAsync(
        string destinatario,
        string assunto,
        string corpo,
        CancellationToken cancellationToken)
    {
        if (!MailAddress.TryCreate(destinatario, out var endereco))
        {
            throw new ArgumentException("O endereço de e-mail destinatário não é válido.", nameof(destinatario));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(assunto);
        ArgumentException.ThrowIfNullOrWhiteSpace(corpo);
        if (assunto.Contains('\r') || assunto.Contains('\n'))
        {
            throw new ArgumentException("O assunto não pode conter quebras de linha.", nameof(assunto));
        }

        var flow = CriarFluxoAutorizacao();
        var token = await _tokenStore.GetAsync<TokenResponse>(TokenUserId);
        if (token is null || string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            throw new InvalidOperationException(
                "A conta remetente ainda não foi autorizada. Inicie a autorização OAuth local.");
        }

        var credential = new UserCredential(flow, TokenUserId, token);
        using var gmail = new GmailService(new Google.Apis.Services.BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "DevRoadmap",
        });

        var mensagem = new Message
        {
            Raw = CriarMensagemRaw(SenderEmail, endereco.Address, assunto, corpo),
        };

        await gmail.Users.Messages.Send(mensagem, "me").ExecuteAsync(cancellationToken);
    }

    private GoogleAuthorizationCodeFlow CriarFluxoAutorizacao()
    {
        using var stream = File.OpenRead(ObterCaminhoCredenciais());
        var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;

        return new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = [GmailService.Scope.GmailSend],
                DataStore = _tokenStore,
            });
    }

    private static string CriarMensagemRaw(
        string remetente,
        string destinatario,
        string assunto,
        string corpo)
    {
        var assuntoCodificado = Convert.ToBase64String(Encoding.UTF8.GetBytes(assunto));
        var corpoCodificado = Convert.ToBase64String(Encoding.UTF8.GetBytes(corpo));
        var corpoEmLinhas = Enumerable.Range(0, (corpoCodificado.Length + 75) / 76)
            .Select(indice => corpoCodificado.Substring(indice * 76, Math.Min(76, corpoCodificado.Length - indice * 76)));

        var mime = string.Join("\r\n",
            $"From: {remetente}",
            $"To: {destinatario}",
            $"Subject: =?UTF-8?B?{assuntoCodificado}?=",
            "MIME-Version: 1.0",
            "Content-Type: text/plain; charset=utf-8",
            "Content-Transfer-Encoding: base64",
            string.Empty,
            string.Join("\r\n", corpoEmLinhas));

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(mime))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
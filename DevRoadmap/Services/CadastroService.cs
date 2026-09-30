using System.Net.Http;
using DevRoadmap.Models;
using DevRoadmap.Repositories;
using DevRoadmap.Services.Email;
using Google;

namespace DevRoadmap.Services;

public enum ResultadoCadastro
{
    CodigoEnviado,
    EmailJaCadastrado,
    ContaCriadaEnvioFalhou,
    DadosInvalidos
}

public enum ResultadoConfirmacaoEmail
{
    Confirmado,
    CodigoInvalido,
    CodigoExpirado,
    LimiteTentativasAtingido,
    ContaNaoPendente,
    ReenvioAguardando,
    CodigoReenviado,
    ReenvioFalhou
}

public class CadastroService
{
    private static readonly TimeSpan ValidadeCodigo = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan IntervaloReenvio = TimeSpan.FromSeconds(60);
    private const int MaximoTentativas = 5;
    private const int QuantidadeDigitosCodigo = 6;

    private readonly UsuarioRepository _usuarioRepository;
    private readonly GmailEmailSender _emailSender;
    private readonly ILogger<CadastroService> _logger;

    public CadastroService(
        UsuarioRepository usuarioRepository,
        GmailEmailSender emailSender,
        ILogger<CadastroService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<ResultadoCadastro> CadastrarUsuarioAsync(
        string nome,
        string email,
        string senha,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nome)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(senha))
        {
            return ResultadoCadastro.DadosInvalidos;
        }

        var codigo = CodigoVerificacaoEmail.GerarCodigo();
        var agora = DateTime.UtcNow;
        var usuario = new Usuario
        {
            Nome = nome.Trim(),
            Email = email.Trim(),
            Senha = UsuarioService.GerarHash(senha),
        };

        var usuarioId = _usuarioRepository.CadastrarPendente(
            usuario,
            CodigoVerificacaoEmail.CalcularHash(codigo),
            agora.Add(ValidadeCodigo));

        if (usuarioId == 0)
        {
            return ResultadoCadastro.EmailJaCadastrado;
        }

        return await EnviarCodigoComResultadoAsync(
            usuarioId,
            usuario.Email,
            codigo,
            cancellationToken);
    }

    public ResultadoConfirmacaoEmail ConfirmarEmail(
        string email,
        string codigo)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return ResultadoConfirmacaoEmail.ContaNaoPendente;
        }

        var usuario = _usuarioRepository.ObterPorEmail(email.Trim());
        if (usuario is null || usuario.EmailVerificado != false)
        {
            return ResultadoConfirmacaoEmail.ContaNaoPendente;
        }

        var verificacao = _usuarioRepository.ObterCodigoVerificacaoEmail(
            usuario.Id,
            FinalidadeVerificacaoEmail.Cadastro);
        if (verificacao is null)
        {
            return ResultadoConfirmacaoEmail.ContaNaoPendente;
        }

        var agora = DateTime.UtcNow;
        if (verificacao.ExpiraEm <= agora)
        {
            return ResultadoConfirmacaoEmail.CodigoExpirado;
        }

        if (verificacao.Tentativas >= MaximoTentativas)
        {
            return ResultadoConfirmacaoEmail.LimiteTentativasAtingido;
        }

        if (string.IsNullOrWhiteSpace(codigo)
            || codigo.Length != QuantidadeDigitosCodigo
            || !codigo.All(char.IsAsciiDigit))
        {
            return RegistrarTentativaInvalida(usuario.Id, agora);
        }

        if (!CodigoVerificacaoEmail.Corresponde(codigo, verificacao.CodigoHash))
        {
            return RegistrarTentativaInvalida(usuario.Id, agora);
        }

        var confirmado = _usuarioRepository.ConfirmarEmailEConsumirCodigo(
            usuario.Id,
            CodigoVerificacaoEmail.CalcularHash(codigo),
            agora);

        return confirmado
            ? ResultadoConfirmacaoEmail.Confirmado
            : ResultadoConfirmacaoEmail.CodigoInvalido;
    }

    public async Task<ResultadoConfirmacaoEmail> ReenviarCodigoAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(email))
        {
            return ResultadoConfirmacaoEmail.ContaNaoPendente;
        }

        var usuario = _usuarioRepository.ObterPorEmail(email.Trim());
        if (usuario is null || usuario.EmailVerificado != false)
        {
            return ResultadoConfirmacaoEmail.ContaNaoPendente;
        }

        var codigo = CodigoVerificacaoEmail.GerarCodigo();
        var agora = DateTime.UtcNow;
        var atualizado = _usuarioRepository.AtualizarCodigoVerificacaoEmail(
            usuario.Id,
            FinalidadeVerificacaoEmail.Cadastro,
            CodigoVerificacaoEmail.CalcularHash(codigo),
            agora.Add(ValidadeCodigo),
            agora.Subtract(IntervaloReenvio));

        if (!atualizado)
        {
            return ResultadoConfirmacaoEmail.ReenvioAguardando;
        }

        var resultado = await EnviarCodigoComResultadoAsync(
            usuario.Id,
            usuario.Email,
            codigo,
            cancellationToken);

        return resultado == ResultadoCadastro.CodigoEnviado
            ? ResultadoConfirmacaoEmail.CodigoReenviado
            : ResultadoConfirmacaoEmail.ReenvioFalhou;
    }

    private ResultadoConfirmacaoEmail RegistrarTentativaInvalida(int usuarioId, DateTime agora)
    {
        var tentativaRegistrada = _usuarioRepository.IncrementarTentativasVerificacaoEmail(
            usuarioId,
            FinalidadeVerificacaoEmail.Cadastro,
            agora);
        if (!tentativaRegistrada)
        {
            var verificacaoAtual = _usuarioRepository.ObterCodigoVerificacaoEmail(
                usuarioId,
                FinalidadeVerificacaoEmail.Cadastro);
            return verificacaoAtual?.Tentativas >= MaximoTentativas
                ? ResultadoConfirmacaoEmail.LimiteTentativasAtingido
                : ResultadoConfirmacaoEmail.CodigoExpirado;
        }

        var verificacao = _usuarioRepository.ObterCodigoVerificacaoEmail(
            usuarioId,
            FinalidadeVerificacaoEmail.Cadastro);
        return verificacao?.Tentativas >= MaximoTentativas
            ? ResultadoConfirmacaoEmail.LimiteTentativasAtingido
            : ResultadoConfirmacaoEmail.CodigoInvalido;
    }

    private async Task<ResultadoCadastro> EnviarCodigoComResultadoAsync(
        int usuarioId,
        string email,
        string codigo,
        CancellationToken cancellationToken)
    {
        var assunto = "Confirme seu e-mail - DevRoadmap";
        var corpo = $"Seu código de confirmação é: {codigo}\n\n" +
                    "Este código expira em 15 minutos. \n" +
                    "Se você não solicitou esta confirmação, ignore esta mensagem.";

        try
        {
            await _emailSender.EnviarEmailAsync(email, assunto, corpo, cancellationToken);
            return ResultadoCadastro.CodigoEnviado;
        }
        catch (GoogleApiException exception)
        {
            _logger.LogError(exception, "O Gmail recusou o envio do código para o usuário {UsuarioId}.", usuarioId);
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "Não foi possível acessar as credenciais de envio para o usuário {UsuarioId}.", usuarioId);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Falha de rede ao enviar o código para o usuário {UsuarioId}.", usuarioId);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "O serviço de e-mail não está pronto para enviar o código ao usuário {UsuarioId}.", usuarioId);
        }

        return ResultadoCadastro.ContaCriadaEnvioFalhou;
    }

}

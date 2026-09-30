using System.Net.Http;
using DevRoadmap.Models;
using DevRoadmap.Repositories;
using DevRoadmap.Services.Email;
using Google;

namespace DevRoadmap.Services;

public enum ResultadoCodigoRecuperacao
{
    Confirmado,
    CodigoInvalido,
    CodigoExpirado,
    LimiteTentativasAtingido
}

public enum ResultadoRedefinicaoSenha
{
    Redefinida,
    IgualAtual,
    CodigoExpirado
}

public sealed class RecuperacaoSenhaService
{
    private static readonly TimeSpan ValidadeCodigo = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan IntervaloReenvio = TimeSpan.FromSeconds(60);
    private const int MaximoTentativas = 5;

    private readonly UsuarioRepository _usuarioRepository;
    private readonly GmailEmailSender _emailSender;
    private readonly RecuperacaoSenhaTokenService _tokenService;
    private readonly ILogger<RecuperacaoSenhaService> _logger;

    public RecuperacaoSenhaService(
        UsuarioRepository usuarioRepository,
        GmailEmailSender emailSender,
        RecuperacaoSenhaTokenService tokenService,
        ILogger<RecuperacaoSenhaService> logger)
    {
        _usuarioRepository = usuarioRepository;
        _emailSender = emailSender;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task SolicitarCodigoAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var usuario = _usuarioRepository.ObterPorEmail(email.Trim());
        if (usuario is null || usuario.EmailVerificado == false)
        {
            return;
        }

        var codigo = CodigoVerificacaoEmail.GerarCodigo();
        var agora = DateTime.UtcNow;
        var criado = _usuarioRepository.CriarCodigoRecuperacaoSenha(
            usuario.Id,
            CodigoVerificacaoEmail.CalcularHash(codigo),
            agora.Add(ValidadeCodigo),
            agora.Subtract(IntervaloReenvio));

        if (!criado)
        {
            return;
        }

        var corpo = $"Seu código para redefinir a senha é: {codigo}\n\n" +
                    "Este código expira em 15 minutos. Se você não solicitou a redefinição, ignore esta mensagem.";
        try
        {
            await _emailSender.EnviarEmailAsync(
                usuario.Email,
                "Redefinição de senha - DevRoadmap",
                corpo,
                cancellationToken);
        }
        catch (GoogleApiException exception)
        {
            _logger.LogError(exception, "O Gmail recusou o envio do código de recuperação para o usuário {UsuarioId}.", usuario.Id);
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "Não foi possível acessar as credenciais de recuperação do usuário {UsuarioId}.", usuario.Id);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Falha de rede ao enviar código de recuperação para o usuário {UsuarioId}.", usuario.Id);
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogError(exception, "O serviço de e-mail não está pronto para recuperar a conta do usuário {UsuarioId}.", usuario.Id);
        }
    }

    public string CriarTokenFluxo(string email)
    {
        return _tokenService.ProtegerEmail(email);
    }

    public bool TentarObterEmailFluxo(string? token, out string email)
    {
        return _tokenService.TentarObterEmail(token, out email);
    }

    public ResultadoConfirmacaoRecuperacao ConfirmarCodigo(string email, string codigo)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new(ResultadoCodigoRecuperacao.CodigoInvalido);
        }

        var usuario = _usuarioRepository.ObterPorEmail(email.Trim());
        if (usuario is null || usuario.EmailVerificado == false)
        {
            return new(ResultadoCodigoRecuperacao.CodigoInvalido);
        }

        var verificacao = _usuarioRepository.ObterCodigoVerificacaoEmail(
            usuario.Id,
            FinalidadeVerificacaoEmail.RecuperacaoSenha);
        if (verificacao is null)
        {
            return new(ResultadoCodigoRecuperacao.CodigoInvalido);
        }

        var agora = DateTime.UtcNow;
        if (verificacao.ExpiraEm <= agora)
        {
            return new(ResultadoCodigoRecuperacao.CodigoExpirado);
        }

        if (verificacao.Tentativas >= MaximoTentativas)
        {
            return new(ResultadoCodigoRecuperacao.LimiteTentativasAtingido);
        }

        if (string.IsNullOrWhiteSpace(codigo)
            || codigo.Length != CodigoVerificacaoEmail.QuantidadeDigitos
            || !codigo.All(char.IsAsciiDigit))
        {
            return new(RegistrarTentativaInvalida(usuario.Id, agora));
        }

        if (!CodigoVerificacaoEmail.Corresponde(codigo, verificacao.CodigoHash))
        {
            return new(RegistrarTentativaInvalida(usuario.Id, agora));
        }

        var token = _tokenService.CriarTokenAutorizacao(
            usuario.Id,
            usuario.Email,
            verificacao.CodigoHash,
            verificacao.ExpiraEm);
        return token is null
            ? new(ResultadoCodigoRecuperacao.CodigoExpirado)
            : new(ResultadoCodigoRecuperacao.Confirmado, token);
    }

    public bool TokenAutorizacaoValido(string? token)
    {
        return _tokenService.TentarObterAutorizacao(token, out _);
    }

    public ResultadoRedefinicaoSenha RedefinirSenha(string token, string novaSenha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(novaSenha);
        if (!_tokenService.TentarObterAutorizacao(token, out var autorizacao))
        {
            return ResultadoRedefinicaoSenha.CodigoExpirado;
        }

        var usuario = _usuarioRepository.ObterPorEmail(autorizacao.Email);

        if (usuario is null
            || usuario.Id != autorizacao.UsuarioId
            || usuario.EmailVerificado == false)
        {
            return ResultadoRedefinicaoSenha.CodigoExpirado;
        }

        if (usuario.Senha.StartsWith("$2", StringComparison.Ordinal)
            && BCrypt.Net.BCrypt.Verify(novaSenha, usuario.Senha))
        {
            return ResultadoRedefinicaoSenha.IgualAtual;
        }

        var senhaHash = UsuarioService.GerarHash(novaSenha);
        var atualizada = _usuarioRepository.RedefinirSenhaComCodigo(
            autorizacao.UsuarioId,
            autorizacao.CodigoHash,
            senhaHash,
            DateTime.UtcNow);

        return atualizada
            ? ResultadoRedefinicaoSenha.Redefinida
            : ResultadoRedefinicaoSenha.CodigoExpirado;
    }

    private ResultadoCodigoRecuperacao RegistrarTentativaInvalida(int usuarioId, DateTime agora)
    {
        var tentativaRegistrada = _usuarioRepository.IncrementarTentativasVerificacaoEmail(
            usuarioId,
            FinalidadeVerificacaoEmail.RecuperacaoSenha,
            agora);

        var verificacao = _usuarioRepository.ObterCodigoVerificacaoEmail(
            usuarioId,
            FinalidadeVerificacaoEmail.RecuperacaoSenha);
        if (verificacao?.Tentativas >= MaximoTentativas)
        {
            return ResultadoCodigoRecuperacao.LimiteTentativasAtingido;
        }

        return tentativaRegistrada
            ? ResultadoCodigoRecuperacao.CodigoInvalido
            : ResultadoCodigoRecuperacao.CodigoExpirado;
    }
}

public sealed record ResultadoConfirmacaoRecuperacao(
    ResultadoCodigoRecuperacao Resultado,
    string? TokenAutorizacao = null);

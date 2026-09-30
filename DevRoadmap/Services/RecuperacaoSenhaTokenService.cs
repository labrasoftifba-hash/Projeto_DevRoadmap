using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace DevRoadmap.Services;

public sealed class RecuperacaoSenhaTokenService
{
    private readonly ITimeLimitedDataProtector _emailProtector;
    private readonly ITimeLimitedDataProtector _authorizationProtector;

    public RecuperacaoSenhaTokenService(IDataProtectionProvider provider)
    {
        _emailProtector = provider
            .CreateProtector("DevRoadmap.RecuperacaoSenha.Email.v1")
            .ToTimeLimitedDataProtector();
        _authorizationProtector = provider
            .CreateProtector("DevRoadmap.RecuperacaoSenha.Autorizacao.v1")
            .ToTimeLimitedDataProtector();
    }

    public string ProtegerEmail(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        var payload = JsonSerializer.SerializeToUtf8Bytes(new EmailPayload(email.Trim()));
        return Convert.ToBase64String(_emailProtector.Protect(payload, TimeSpan.FromMinutes(15)));
    }

    public bool TentarObterEmail(string? token, out string email)
    {
        email = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var protectedToken = Convert.FromBase64String(token);
            var payload = JsonSerializer.Deserialize<EmailPayload>(
                _emailProtector.Unprotect(protectedToken, out _));
            if (string.IsNullOrWhiteSpace(payload?.Email))
            {
                return false;
            }

            email = payload.Email;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public string? CriarTokenAutorizacao(int usuarioId, string email, byte[] codigoHash, DateTime expiraEm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(codigoHash);
        if (codigoHash.Length != 32)
        {
            throw new ArgumentException("O hash do código deve conter 32 bytes.", nameof(codigoHash));
        }

        var validade = expiraEm - DateTime.UtcNow;
        if (validade <= TimeSpan.Zero)
        {
            return null;
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new AuthorizationPayload(usuarioId, email.Trim(), Convert.ToBase64String(codigoHash)));
        return Convert.ToBase64String(_authorizationProtector.Protect(payload, validade));
    }

    public bool TentarObterAutorizacao(string? token, out AutorizacaoRecuperacao autorizacao)
    {
        autorizacao = default!;
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var protectedToken = Convert.FromBase64String(token);
            var payload = JsonSerializer.Deserialize<AuthorizationPayload>(
                _authorizationProtector.Unprotect(protectedToken, out _));
            if (payload is null
                || payload.UsuarioId <= 0
                || string.IsNullOrWhiteSpace(payload.Email)
                || string.IsNullOrWhiteSpace(payload.CodigoHash))
            {
                return false;
            }

            var codigoHash = Convert.FromBase64String(payload.CodigoHash);
            if (codigoHash.Length != 32)
            {
                return false;
            }

            autorizacao = new(payload.UsuarioId, payload.Email, codigoHash);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed record EmailPayload(string Email);

    private sealed record AuthorizationPayload(int UsuarioId, string Email, string CodigoHash);
}

public sealed record AutorizacaoRecuperacao(int UsuarioId, string Email, byte[] CodigoHash);

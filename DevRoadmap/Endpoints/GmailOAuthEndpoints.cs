using System.Security.Cryptography;
using System.Text;
using DevRoadmap.Services.Email;

namespace DevRoadmap.Endpoints;

public static class GmailOAuthEndpoints
{
    private const string _callbackPath = "/oauth/gmail/callback";
    private const string _stateCookieName = "DevRoadmap.GmailOAuthState";

    public static IEndpointRouteBuilder MapGmailOAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/oauth/gmail/start", IniciarAutorizacao);
        endpoints.MapGet(_callbackPath, FinalizarAutorizacaoAsync);

        return endpoints;
    }

    private static IResult IniciarAutorizacao(HttpContext context, GmailEmailSender emailSender)
    {
        if (!ehAcessoLocal(context))
        {
            return Results.Forbid();
        }

        if (!context.Request.IsHttps)
        {
            return Results.BadRequest("Inicie a autorização usando https://localhost:7246/oauth/gmail/start.");
        }

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        context.Response.Cookies.Append(
            _stateCookieName,
            state,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(10),
                Path = _callbackPath,
            });

        return Results.Redirect(emailSender.CriarUrlAutorizacao(state));
    }

    private static async Task<IResult> FinalizarAutorizacaoAsync(
        HttpContext context,
        GmailEmailSender emailSender,
        ILoggerFactory loggerFactory,
        string? code,
        string? state,
        string? error,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(nameof(GmailOAuthEndpoints));
        context.Response.Cookies.Delete(
            _stateCookieName,
            new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = _callbackPath,
            });

        if (!ehAcessoLocal(context))
        {
            return Results.Forbid();
        }

        var stateCookie = context.Request.Cookies[_stateCookieName];
        if (string.IsNullOrEmpty(state)
            || string.IsNullOrEmpty(stateCookie)
            || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(state),
                Encoding.UTF8.GetBytes(stateCookie)))
        {
            return Results.BadRequest("A validação OAuth expirou ou não corresponde. Inicie a autorização novamente.");
        }

        if (!string.IsNullOrEmpty(error))
        {
            return Results.BadRequest("A autorização da conta Google foi recusada ou cancelada.");
        }

        if (string.IsNullOrEmpty(code))
        {
            return Results.BadRequest("O Google não retornou um código de autorização.");
        }

        try
        {
            await emailSender.AutorizarAsync(code, cancellationToken);
        }
        catch (Google.Apis.Auth.OAuth2.Responses.TokenResponseException exception)
        {
            logger.LogError(exception, "Falha na troca do código OAuth do Gmail por token de acesso.");
            return Results.Problem(
                "O Google não concluiu a autorização do remetente. Confira o cliente OAuth e o URI de retorno configurado.",
                statusCode: StatusCodes.Status502BadGateway);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogError(exception, "A autorização OAuth do Gmail não retornou um refresh token.");
            return Results.Problem(
                "O Google não forneceu uma autorização renovável. Tente novamente e confirme o consentimento da conta remetente.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Text("Conta remetente autorizada. Você pode fechar esta página.");
    }

    private static bool ehAcessoLocal(HttpContext context)
    {
        var remoteAddress = context.Connection.RemoteIpAddress;
        return remoteAddress is not null && System.Net.IPAddress.IsLoopback(remoteAddress);
    }
}

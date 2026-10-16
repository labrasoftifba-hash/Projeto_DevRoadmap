using System.Security.Claims;
using DevRoadmap.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DevRoadmap.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/auth/login", LoginAsync).AllowAnonymous();
        endpoints.MapPost("/auth/logout", LogoutAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        UsuarioService usuarioService,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(AuthenticationEndpoints));

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException exception)
        {
            logger.LogWarning(exception, "A tentativa de login falhou na validação antiforgery.");
            return Results.BadRequest("Não foi possível validar o formulário. Atualize a página e tente novamente.");
        }

        var form = await httpContext.Request.ReadFormAsync(httpContext.RequestAborted);
        var usuario = usuarioService.Autenticar(form["Email"].ToString(), form["Senha"].ToString());

        if (usuario is null)
        {
            return Results.Redirect("/login?erro=credenciais");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        return Results.Redirect("/student/dashboard");
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        IAntiforgery antiforgery,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger(nameof(AuthenticationEndpoints));

        try
        {
            await antiforgery.ValidateRequestAsync(httpContext);
        }
        catch (AntiforgeryValidationException exception)
        {
            logger.LogWarning(exception, "A tentativa de logout falhou na validação antiforgery.");
            return Results.BadRequest("Não foi possível validar a solicitação. Atualize a página e tente novamente.");
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Google.Apis.Auth.OAuth2.Responses;
using DevRoadmap.Components;
using DevRoadmap.Endpoints;
using DevRoadmap.Repositories;
using DevRoadmap.Services;
using DevRoadmap.Services.Email;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using DevRoadmap.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "DevRoadmap.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CadastroService>();
builder.Services.AddScoped<RecuperacaoSenhaService>();
builder.Services.AddSingleton<RecuperacaoSenhaTokenService>();
builder.Services.AddSingleton<GmailEmailSender>();
builder.Services.AddDbContext<AppDbContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapAuthenticationEndpoints();
if (app.Environment.IsDevelopment())
{
    app.MapGmailOAuthEndpoints();
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        _ = AbrirAutorizacaoGmailSeNecessarioAsync(app);
    });
}
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static async Task AbrirAutorizacaoGmailSeNecessarioAsync(WebApplication app)
{
    try
    {
        var emailSender = app.Services.GetRequiredService<GmailEmailSender>();
        emailSender.ObterCaminhoCredenciais();

        if (await emailSender.PossuiRefreshTokenValidoAsync(CancellationToken.None))
        {
            return;
        }

        const string enderecoHttpsLocal = "https://localhost:7246";
        if (!app.Urls.Contains(enderecoHttpsLocal, StringComparer.OrdinalIgnoreCase))
        {
            app.Logger.LogWarning(
                "A autorização automática do Gmail foi ignorada: inicie a aplicação pelo perfil HTTPS em {Endereco}.",
                enderecoHttpsLocal);
            return;
        }

        using var navegador = Process.Start(new ProcessStartInfo(
            $"{enderecoHttpsLocal}/oauth/gmail/start")
        {
            UseShellExecute = true,
        });
        if (navegador is null)
        {
            throw new InvalidOperationException(
                "O Windows não iniciou o navegador para a autorização do Gmail.");
        }

        app.Logger.LogInformation(
            "Não há refresh token do Gmail. O navegador foi aberto para autorizar a conta remetente.");
    }
    catch (FileNotFoundException)
    {
        app.Logger.LogWarning(
            "A autorização automática do Gmail foi ignorada porque o arquivo de credenciais OAuth não foi encontrado. " +
            "Configure web-client-secret.json para habilitar o envio de e-mails.");
    }
    catch (Exception exception) when (
        exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or InvalidOperationException
            or Win32Exception
            or HttpRequestException
            or TokenResponseException)
    {
        app.Logger.LogError(
            exception,
            "Não foi possível iniciar automaticamente a autorização local do Gmail.");
    }
}

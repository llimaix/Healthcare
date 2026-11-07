using System.Text;
using ClinicaCore.Gateway.Reenvio;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Prefijo de variables de entorno: CLINICACORE_ con separador __ para secciones.
builder.Configuration.AddEnvironmentVariables(prefix: "CLINICACORE_");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Misma validacion que la Api, resuelta de la configuracion efectiva (runtime).
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var secreto = configuration["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(secreto))
        {
            throw new InvalidOperationException(
                "La configuracion Jwt:Secret es obligatoria para validar tokens.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secreto)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// Cliente tipado hacia el servicio de dominio (BFF).
var apiBaseUrl = builder.Configuration["Services:ApiBaseUrl"] ?? "http://localhost:5080";
builder.Services.AddHttpClient<ClinicaCoreApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

// Reenvio de la superficie publica al servicio de dominio via el HttpClient tipado.
// Reproduce estado, cabeceras relevantes y cuerpo de la respuesta de la Api.
static async Task Reproducir(HttpResponseMessage origen, HttpResponse destino, CancellationToken ct)
{
    destino.StatusCode = (int)origen.StatusCode;
    if (origen.Content.Headers.ContentType is { } tipo)
    {
        destino.ContentType = tipo.ToString();
    }
    await origen.Content.CopyToAsync(destino.Body, ct);
}

// Autenticacion (login/registro) sin token; el resto de /api requiere JWT valido.
app.Map("/api/auth/{**resto}", async (
    HttpContext contexto, ClinicaCoreApiClient api, CancellationToken ct) =>
{
    using var respuesta = await api.ReenviarAsync(contexto.Request, ct);
    await Reproducir(respuesta, contexto.Response, ct);
}).AllowAnonymous();

app.Map("/api/{**resto}", async (
    HttpContext contexto, ClinicaCoreApiClient api, CancellationToken ct) =>
{
    using var respuesta = await api.ReenviarAsync(contexto.Request, ct);
    await Reproducir(respuesta, contexto.Response, ct);
}).RequireAuthorization();

app.Run();

using System.Text;
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
builder.Services.AddHttpClient("ClinicaCoreApi", client =>
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

app.Run();

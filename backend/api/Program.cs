using System.Text;
using ClinicaCore.Api.Auth;
using ClinicaCore.Api.Datos;
using ClinicaCore.Api.Seguridad;
using ClinicaCore.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Prefijo de variables de entorno: CLINICACORE_ con separador __ para secciones.
builder.Configuration.AddEnvironmentVariables(prefix: "CLINICACORE_");

var connectionString = builder.Configuration.GetConnectionString("ClinicaCore")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexion ConnectionStrings:ClinicaCore.");

builder.Services.AddDbContext<ClinicaCoreDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Los parametros de validacion se resuelven de la configuracion efectiva (runtime)
// para usar la misma clave y emisor que JwtTokenService al firmar.
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

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Siembra de roles al arrancar.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClinicaCoreDbContext>();
    await RolSeeder.SembrarAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.MapControllers();

app.Run();

/// <summary>Punto de entrada expuesto para las pruebas de integracion.</summary>
public partial class Program;

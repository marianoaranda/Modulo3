using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Stock.Api.Data;
using Stock.Api.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("StockDb")));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Falta la sección de configuración 'Jwt'.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// RF-12: todo endpoint exige JWT válido salvo que se marque [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var log = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Arranque");

    // Sólo se prende en docker-compose, donde la base arranca vacía y nadie más la migra.
    // Fuera de ahí las migraciones son un paso explícito (`dotnet ef database update`),
    // para que varias instancias no compitan por migrar al mismo tiempo.
    if (cfg.GetValue<bool>("ApplyMigrationsOnStartup"))
    {
        await MigrarConEsperaAsync(db, log);
    }

    if (await db.Database.CanConnectAsync() && !(await db.Database.GetPendingMigrationsAsync()).Any())
    {
        await DbSeeder.SeedAsync(db, cfg["Seed:UsuarioAdmin"] ?? "admin", cfg["Seed:PasswordAdmin"] ?? "Admin1234");
    }
    else
    {
        log.LogWarning("Base inaccesible o con migraciones pendientes: se omite el seed. Ejecutá 'dotnet ef database update --project src/Stock.Api'.");
    }
}

/// <summary>
/// El healthcheck del contenedor de SQL Server puede dar verde unos segundos antes
/// de aceptar conexiones, así que reintentamos en vez de morir en el arranque.
/// </summary>
static async Task MigrarConEsperaAsync(AppDbContext db, ILogger log)
{
    const int intentos = 10;
    for (var intento = 1; intento <= intentos; intento++)
    {
        try
        {
            await db.Database.MigrateAsync();
            return;
        }
        catch (Exception ex) when (intento < intentos)
        {
            log.LogWarning("La base todavía no responde (intento {Intento}/{Total}): {Mensaje}", intento, intentos, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Dentro del contenedor sólo escuchamos HTTP: el redirect no tendría puerto destino
// y ensuciaría el log con un warning por request.
if (!app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Expuesta como partial para que WebApplicationFactory pueda hospedar la API en los tests.</summary>
public partial class Program { }

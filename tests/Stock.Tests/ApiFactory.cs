using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stock.Api.Data;
using Stock.Api.Domain;
using Stock.Api.Security;

namespace Stock.Tests;

/// <summary>
/// Hospeda la API real contra una base Sqlite en memoria, para ejercitar
/// el pipeline completo (autenticación incluida) sin depender de SQL Server.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string UsuarioValido = "vendedor1";
    public const string PasswordValida = "Vendedor123";

    private SqliteConnection? _conexion;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);

            _conexion = new SqliteConnection("DataSource=:memory:");
            _conexion.Open();

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_conexion));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            Sembrar(db);
        });
    }

    private static void Sembrar(AppDbContext db)
    {
        var perfil = new Perfil { Descripcion = "vendedor" };
        db.Perfiles.Add(perfil);
        db.SaveChanges();

        var (hash, salt) = PasswordHasher.Hash(PasswordValida);
        db.Usuarios.Add(new Usuario
        {
            NombreUsuario = UsuarioValido,
            NombreCompleto = "Vendedor de prueba",
            Hash = hash,
            Salt = salt,
            PerfilId = perfil.PerfilId
        });
        db.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _conexion?.Dispose();
        }
    }
}

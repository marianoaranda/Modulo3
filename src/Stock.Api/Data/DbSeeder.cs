using Microsoft.EntityFrameworkCore;
using Stock.Api.Domain;
using Stock.Api.Security;

namespace Stock.Api.Data;

public static class DbSeeder
{
    /// <summary>
    /// Deja el sistema utilizable en un entorno nuevo: los tres perfiles del PRD
    /// y un usuario administrador inicial. Es idempotente.
    /// </summary>
    public static async Task SeedAsync(AppDbContext db, string usuarioAdmin, string passwordAdmin)
    {
        foreach (var descripcion in new[] { Perfil.Administrador, "administrativo", "vendedor" })
        {
            if (!await db.Perfiles.AnyAsync(p => p.Descripcion == descripcion))
            {
                db.Perfiles.Add(new Perfil { Descripcion = descripcion });
            }
        }
        await db.SaveChangesAsync();

        if (!await db.Usuarios.AnyAsync())
        {
            var perfilAdmin = await db.Perfiles.SingleAsync(p => p.Descripcion == Perfil.Administrador);
            var (hash, salt) = PasswordHasher.Hash(passwordAdmin);

            db.Usuarios.Add(new Usuario
            {
                NombreUsuario = usuarioAdmin,
                NombreCompleto = "Administrador del sistema",
                Hash = hash,
                Salt = salt,
                PerfilId = perfilAdmin.PerfilId
            });
            await db.SaveChangesAsync();
        }
    }
}

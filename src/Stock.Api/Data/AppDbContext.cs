using Microsoft.EntityFrameworkCore;
using Stock.Api.Domain;

namespace Stock.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Articulo> Articulos => Set<Articulo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Perfil>(e =>
        {
            e.ToTable("Perfiles");
            e.HasKey(p => p.PerfilId);
            e.Property(p => p.Descripcion).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(u => u.UsuarioId);
            e.Property(u => u.NombreUsuario).HasColumnName("Usuario").HasMaxLength(50).IsRequired();
            e.HasIndex(u => u.NombreUsuario).IsUnique();
            e.Property(u => u.NombreCompleto).HasMaxLength(200).IsRequired();
            e.Property(u => u.Hash).HasMaxLength(200).IsRequired();
            e.Property(u => u.Salt).HasMaxLength(200).IsRequired();
            e.HasOne(u => u.Perfil)
             .WithMany(p => p.Usuarios)
             .HasForeignKey(u => u.PerfilId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Articulo>(e =>
        {
            e.ToTable("Articulos");
            e.HasKey(a => a.ArticuloId);
            e.Property(a => a.Codigo).HasMaxLength(50).IsRequired();
            e.HasIndex(a => a.Codigo).IsUnique();
            e.Property(a => a.Descripcion).HasMaxLength(200).IsRequired();
            e.Property(a => a.PrecioCosto).HasColumnType("decimal(18,2)");
            e.Property(a => a.Margen).HasColumnType("decimal(9,2)");
            // PrecioVenta se deriva de Costo y Margen (RF-16): no es columna.
            e.Ignore(a => a.PrecioVenta);
        });
    }
}

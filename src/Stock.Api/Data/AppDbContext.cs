using Microsoft.EntityFrameworkCore;
using Stock.Api.Domain;

namespace Stock.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Perfil> Perfiles => Set<Perfil>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

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
    }
}

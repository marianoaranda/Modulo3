namespace Stock.Api.Domain;

/// <summary>Perfil de seguridad (RF-01 a RF-03).</summary>
public class Perfil
{
    public int PerfilId { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();

    /// <summary>Descripción del perfil con acceso a la carga de usuarios (RF-10).</summary>
    public const string Administrador = "administrador";
}

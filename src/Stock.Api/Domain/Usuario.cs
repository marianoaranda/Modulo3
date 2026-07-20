namespace Stock.Api.Domain;

/// <summary>Usuario del sistema (RF-04 a RF-06). La contraseña vive sólo como Hash + Salt (RF-07, RF-08).</summary>
public class Usuario
{
    public int UsuarioId { get; set; }

    /// <summary>Nombre de login. Se mapea a la columna "Usuario" del PRD.</summary>
    public string NombreUsuario { get; set; } = string.Empty;

    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Hash PBKDF2 de la contraseña, en Base64.</summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>Salt aleatorio propio de este usuario, en Base64.</summary>
    public string Salt { get; set; } = string.Empty;

    public int PerfilId { get; set; }
    public Perfil? Perfil { get; set; }
}

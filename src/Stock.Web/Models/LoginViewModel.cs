using System.ComponentModel.DataAnnotations;

namespace Stock.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingresá el usuario.")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá la contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

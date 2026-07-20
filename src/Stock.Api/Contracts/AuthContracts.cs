using System.ComponentModel.DataAnnotations;

namespace Stock.Api.Contracts;

public record LoginRequest
{
    [Required]
    public string Usuario { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse(string Token, DateTime ExpiraUtc, string NombreCompleto, string Perfil);

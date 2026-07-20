using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock.Api.Contracts;
using Stock.Api.Data;
using Stock.Api.Security;

namespace Stock.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    /// <summary>Mensaje único para usuario inexistente y contraseña incorrecta (AC-12, AC-13).</summary>
    public const string CredencialesInvalidas = "Usuario o contraseña incorrectos";

    private readonly AppDbContext _db;
    private readonly IJwtTokenService _tokens;

    public AuthController(AppDbContext db, IJwtTokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    /// <summary>Inicio de sesión (RF-11). Único endpoint anónimo del sistema (RF-12).</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = await _db.Usuarios
            .Include(u => u.Perfil)
            .SingleOrDefaultAsync(u => u.NombreUsuario == request.Usuario);

        if (usuario is null || !PasswordHasher.Verify(request.Password, usuario.Hash, usuario.Salt))
        {
            return Unauthorized(new { mensaje = CredencialesInvalidas });
        }

        var (token, expira) = _tokens.Generar(usuario);
        return Ok(new LoginResponse(token, expira, usuario.NombreCompleto, usuario.Perfil?.Descripcion ?? string.Empty));
    }

    /// <summary>Endpoint protegido mínimo: devuelve la identidad del token vigente.</summary>
    [HttpGet("yo")]
    public ActionResult<object> Yo() => Ok(new { usuario = User.Identity?.Name });
}

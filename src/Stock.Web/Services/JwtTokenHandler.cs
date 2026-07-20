using System.Net.Http.Headers;
using Stock.Web.Security;

namespace Stock.Web.Services;

/// <summary>
/// Adjunta el JWT de la sesión a cada llamada saliente a la API (RF-12).
/// El token viaja en un claim de la cookie de autenticación, así que se lee
/// del HttpContext del request que el sitio está atendiendo en ese momento.
/// </summary>
public class JwtTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _contexto;

    public JwtTokenHandler(IHttpContextAccessor contexto) => _contexto = contexto;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _contexto.HttpContext?.User.FindFirst(SessionClaims.Token)?.Value;

        // Si no hay token seguimos igual: el login es anónimo y para el resto
        // la API responde 401, que es lo que el caller espera manejar.
        if (!string.IsNullOrEmpty(token) && request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

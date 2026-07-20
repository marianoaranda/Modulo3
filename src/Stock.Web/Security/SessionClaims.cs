namespace Stock.Web.Security;

/// <summary>Claims propios que guardamos en la cookie de sesión del sitio.</summary>
public static class SessionClaims
{
    /// <summary>JWT emitido por la API, que se reusa en cada llamada saliente (RF-12).</summary>
    public const string Token = "stock:jwt";
}

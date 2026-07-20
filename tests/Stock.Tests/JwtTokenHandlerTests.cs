using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Stock.Web.Security;
using Stock.Web.Services;

namespace Stock.Tests;

/// <summary>
/// Ejercita el handler del sitio contra la API real: lo que se prueba es que el
/// token guardado en la sesión llegue efectivamente al pipeline de la API (RF-12).
/// </summary>
[TestFixture]
public class JwtTokenHandlerTests
{
    private ApiFactory _factory = null!;

    [OneTimeSetUp]
    public void SetUp() => _factory = new ApiFactory();

    [OneTimeTearDown]
    public void TearDown() => _factory.Dispose();

    [Test]
    public async Task ConSesion_AdjuntaElTokenYLaApiAutoriza()
    {
        using var client = CrearClienteDelSitio(await ObtenerTokenAsync());

        var respuesta = await client.GetAsync("api/auth/yo");

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(ApiFactory.UsuarioValido));
    }

    [Test]
    public async Task SinSesion_NoAdjuntaNadaYLaApiRechaza()
    {
        using var client = CrearClienteDelSitio(token: null);

        var respuesta = await client.GetAsync("api/auth/yo");

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// Arma el HttpClient tal como queda configurado en Stock.Web: el handler
    /// encima del transporte, leyendo el claim de un HttpContext con o sin sesión.
    /// </summary>
    private HttpClient CrearClienteDelSitio(string? token)
    {
        var identidad = token is null
            ? new ClaimsIdentity()
            : new ClaimsIdentity(new[] { new Claim(SessionClaims.Token, token) }, "Cookies");

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
        };

        var handler = new JwtTokenHandler(accessor) { InnerHandler = _factory.Server.CreateHandler() };

        return new HttpClient(handler) { BaseAddress = _factory.Server.BaseAddress };
    }

    private async Task<string> ObtenerTokenAsync()
    {
        using var client = _factory.CreateClient();
        var respuesta = await client.PostAsJsonAsync("/api/auth/login",
            new { Usuario = ApiFactory.UsuarioValido, Password = ApiFactory.PasswordValida });

        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<LoginPayload>();
        return cuerpo!.Token;
    }

    private record LoginPayload(string Token, DateTime ExpiraUtc, string NombreCompleto, string Perfil);
}

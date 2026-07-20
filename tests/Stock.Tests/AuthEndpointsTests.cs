using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Stock.Api.Controllers;

namespace Stock.Tests;

[TestFixture]
public class AuthEndpointsTests
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task Login_ConUsuarioInexistente_RechazaConElMensajeGenerico()
    {
        // AC-12 (RF-11)
        var respuesta = await _client.PostAsJsonAsync("/api/auth/login",
            new { Usuario = "noExiste", Password = "Cualquiera123" });

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(AuthController.CredencialesInvalidas));
    }

    [Test]
    public async Task Login_ConPasswordIncorrecta_RechazaConElMensajeGenerico()
    {
        // AC-13 (RF-11)
        var respuesta = await _client.PostAsJsonAsync("/api/auth/login",
            new { Usuario = ApiFactory.UsuarioValido, Password = "PasswordMala1" });

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(AuthController.CredencialesInvalidas));
    }

    [Test]
    public async Task Login_ConCredencialesCorrectas_DevuelveUnTokenUsable()
    {
        // AC-14 (RF-11)
        var token = await ObtenerTokenAsync();

        Assert.That(token, Is.Not.Empty);
    }

    [Test]
    public async Task EndpointProtegido_SinToken_Devuelve401()
    {
        // AC-15 (RF-12)
        var respuesta = await _client.GetAsync("/api/auth/yo");

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task EndpointProtegido_ConTokenInvalido_Devuelve401()
    {
        // AC-15 (RF-12)
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/yo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "token.no.valido");

        var respuesta = await _client.SendAsync(request);

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task EndpointProtegido_ConTokenValido_AutorizaElAcceso()
    {
        // AC-15 (RF-12), caso positivo
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/yo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await ObtenerTokenAsync());

        var respuesta = await _client.SendAsync(request);

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(ApiFactory.UsuarioValido));
    }

    private async Task<string> ObtenerTokenAsync()
    {
        var respuesta = await _client.PostAsJsonAsync("/api/auth/login",
            new { Usuario = ApiFactory.UsuarioValido, Password = ApiFactory.PasswordValida });

        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<LoginPayload>();
        return cuerpo!.Token;
    }

    private record LoginPayload(string Token, DateTime ExpiraUtc, string NombreCompleto, string Perfil);
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Stock.Api.Controllers;
using Stock.Api.Domain;

namespace Stock.Tests;

/// <summary>ABM de artículos contra la API real sobre SQLite en memoria (RF-13 a RF-19).</summary>
[TestFixture]
public class ArticulosEndpointsTests
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        // Base fresca por test: cada uno arranca sin artículos cargados.
        _factory = new ApiFactory();
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await ObtenerTokenAsync());
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static ArticuloRequest ArticuloValido(string codigo = "A001") => new(codigo, "Taladro", 100m, 50m, 5, 10, 20);

    [Test]
    public async Task SinToken_Devuelve401()
    {
        // RF-12: el ABM también exige sesión.
        using var anonimo = _factory.CreateClient();

        var respuesta = await anonimo.GetAsync("/api/articulos");

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Crear_ArticuloValido_QuedaPersistidoYRecuperable()
    {
        // AC-16 (RF-13)
        var creado = await CrearAsync(ArticuloValido());

        var recuperado = await _client.GetFromJsonAsync<ArticuloResponse>($"/api/articulos/{creado.ArticuloId}");

        Assert.That(recuperado, Is.Not.Null);
        Assert.That(recuperado!.Codigo, Is.EqualTo("A001"));
        // RF-16: 100 × (1 + 50/100) = 150
        Assert.That(recuperado.PrecioVenta, Is.EqualTo(150m));
    }

    [Test]
    public async Task Eliminar_ArticuloExistente_DejaDeSerRecuperable()
    {
        // AC-17 (RF-14)
        var creado = await CrearAsync(ArticuloValido());

        var baja = await _client.DeleteAsync($"/api/articulos/{creado.ArticuloId}");
        Assert.That(baja.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var recuperar = await _client.GetAsync($"/api/articulos/{creado.ArticuloId}");
        Assert.That(recuperar.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Modificar_ArticuloExistente_PersisteElCambio()
    {
        // AC-18 (RF-15)
        var creado = await CrearAsync(ArticuloValido());
        var modificado = creado with { Descripcion = "Taladro percutor", Margen = 100m };

        var respuesta = await _client.PutAsJsonAsync($"/api/articulos/{creado.ArticuloId}", ToRequest(modificado));
        respuesta.EnsureSuccessStatusCode();

        var recuperado = await _client.GetFromJsonAsync<ArticuloResponse>($"/api/articulos/{creado.ArticuloId}");
        Assert.That(recuperado!.Descripcion, Is.EqualTo("Taladro percutor"));
        // RF-16 recalculado: 100 × (1 + 100/100) = 200
        Assert.That(recuperado.PrecioVenta, Is.EqualTo(200m));
    }

    [Test]
    public async Task Crear_ConCodigoDuplicado_RechazaYNoGraba()
    {
        // AC-20 (RF-17)
        await CrearAsync(ArticuloValido("DUP"));

        var respuesta = await _client.PostAsJsonAsync("/api/articulos", ArticuloValido("DUP"));

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(ArticulosController.CodigoDuplicado));
    }

    [Test]
    public async Task Modificar_HaciaUnCodigoDeOtroArticulo_Rechaza()
    {
        // AC-20 (RF-17): la unicidad también se controla al modificar.
        await CrearAsync(ArticuloValido("A001"));
        var segundo = await CrearAsync(ArticuloValido("A002"));

        var respuesta = await _client.PutAsJsonAsync($"/api/articulos/{segundo.ArticuloId}",
            ArticuloValido("A001"));

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Modificar_ConElMismoCodigo_NoLoTomaComoDuplicado()
    {
        // RF-17: un artículo no choca consigo mismo al guardarse sin cambiar el código.
        var creado = await CrearAsync(ArticuloValido("A001"));

        var respuesta = await _client.PutAsJsonAsync($"/api/articulos/{creado.ArticuloId}",
            ArticuloValido("A001") with { Descripcion = "Otra descripción" });

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Crear_ConValorNegativo_RechazaYNoGraba()
    {
        // AC-21 (RF-18)
        var respuesta = await _client.PostAsJsonAsync("/api/articulos",
            ArticuloValido() with { PrecioCosto = -1m });

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(ArticuloValidator.ValoresNegativos));
    }

    [Test]
    public async Task Crear_ConNivelesDesordenados_RechazaYNoGraba()
    {
        // AC-22 (RF-19): Mínimo 30 > Ideal 20 rompe el orden.
        var respuesta = await _client.PostAsJsonAsync("/api/articulos",
            ArticuloValido() with { StockMinimo = 30, PuntoPedido = 25, StockIdeal = 20 });

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(ArticuloValidator.OrdenDeStock));
    }

    private async Task<ArticuloResponse> CrearAsync(ArticuloRequest request)
    {
        var respuesta = await _client.PostAsJsonAsync("/api/articulos", request);
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<ArticuloResponse>())!;
    }

    private static ArticuloRequest ToRequest(ArticuloResponse a) =>
        new(a.Codigo, a.Descripcion, a.PrecioCosto, a.Margen, a.StockMinimo, a.PuntoPedido, a.StockIdeal);

    private async Task<string> ObtenerTokenAsync()
    {
        var respuesta = await _client.PostAsJsonAsync("/api/auth/login",
            new { Usuario = ApiFactory.UsuarioValido, Password = ApiFactory.PasswordValida });
        respuesta.EnsureSuccessStatusCode();
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<LoginPayload>();
        return cuerpo!.Token;
    }

    private record LoginPayload(string Token, DateTime ExpiraUtc, string NombreCompleto, string Perfil);

    /// <summary>Espejo local de Stock.Api.Contracts.ArticuloRequest para armar los cuerpos.</summary>
    private record ArticuloRequest(
        string Codigo, string Descripcion, decimal PrecioCosto, decimal Margen,
        int StockMinimo, int PuntoPedido, int StockIdeal);

    private record ArticuloResponse(
        int ArticuloId, string Codigo, string Descripcion, decimal PrecioCosto, decimal Margen,
        decimal PrecioVenta, int StockMinimo, int PuntoPedido, int StockIdeal);
}

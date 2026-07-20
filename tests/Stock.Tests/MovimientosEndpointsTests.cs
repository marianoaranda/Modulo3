using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Stock.Api.Controllers;
using Stock.Api.Domain;

namespace Stock.Tests;

/// <summary>
/// ABM de movimientos contra la API real sobre SQLite en memoria (RF-20 a RF-24).
/// Ejercita de paso el StockService: el rechazo de ventas depende del saldo calculado.
/// </summary>
[TestFixture]
public class MovimientosEndpointsTests
{
    private ApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
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

    [Test]
    public async Task SinToken_Devuelve401()
    {
        // RF-12
        using var anonimo = _factory.CreateClient();
        var respuesta = await anonimo.GetAsync("/api/movimientos");
        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Crear_MovimientoValido_QuedaPersistidoConEncabezadoYDetalle()
    {
        // AC-23 (RF-20)
        await CrearArticuloAsync("A001");

        var creado = await CrearMovimientoAsync(new MovReq("Compra", 1, DateTime.UtcNow,
            new() { new LineaReq("A001", 10, 25m) }));

        var recuperado = await _client.GetFromJsonAsync<MovResp>($"/api/movimientos/{creado.MovimientoId}");
        Assert.That(recuperado, Is.Not.Null);
        Assert.That(recuperado!.Detalles, Has.Count.EqualTo(1));
        Assert.That(recuperado.Detalles[0].Codigo, Is.EqualTo("A001"));
        // El Precio Total lo calcula el servidor: 10 × 25 = 250.
        Assert.That(recuperado.Detalles[0].PrecioTotal, Is.EqualTo(250m));
    }

    [Test]
    public async Task Eliminar_MovimientoExistente_DejaDeSerRecuperable()
    {
        // AC-24 (RF-21)
        await CrearArticuloAsync("A001");
        var creado = await CrearMovimientoAsync(new MovReq("Compra", 1, DateTime.UtcNow,
            new() { new LineaReq("A001", 5, 10m) }));

        var baja = await _client.DeleteAsync($"/api/movimientos/{creado.MovimientoId}");
        Assert.That(baja.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var recuperar = await _client.GetAsync($"/api/movimientos/{creado.MovimientoId}");
        Assert.That(recuperar.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Modificar_MovimientoExistente_PersisteElCambio()
    {
        // AC-25 (RF-22)
        await CrearArticuloAsync("A001");
        var creado = await CrearMovimientoAsync(new MovReq("Compra", 1, DateTime.UtcNow,
            new() { new LineaReq("A001", 5, 10m) }));

        var respuesta = await _client.PutAsJsonAsync($"/api/movimientos/{creado.MovimientoId}",
            new MovReq("Compra", 1, DateTime.UtcNow, new() { new LineaReq("A001", 8, 12m) }));
        respuesta.EnsureSuccessStatusCode();

        var recuperado = await _client.GetFromJsonAsync<MovResp>($"/api/movimientos/{creado.MovimientoId}");
        Assert.That(recuperado!.Detalles[0].Cantidad, Is.EqualTo(8));
        Assert.That(recuperado.Detalles[0].PrecioTotal, Is.EqualTo(96m)); // 8 × 12
    }

    [TestCase(0)]
    [TestCase(-2)]
    public async Task Crear_ConCantidadNoPositiva_Rechaza(int cantidad)
    {
        // AC-26 (RF-23)
        await CrearArticuloAsync("A001");

        var respuesta = await _client.PostAsJsonAsync("/api/movimientos",
            new MovReq("Compra", 1, DateTime.UtcNow, new() { new LineaReq("A001", cantidad, 10m) }));

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(MovimientoValidator.CantidadInvalida));
    }

    [Test]
    public async Task Crear_VentaQueDejariaStockNegativo_RechazaYNoGraba()
    {
        // AC-27 (RF-24): compro 5, intento vender 6.
        await CrearArticuloAsync("A001");
        await CrearMovimientoAsync(new MovReq("Compra", 1, DateTime.UtcNow, new() { new LineaReq("A001", 5, 10m) }));

        var respuesta = await _client.PostAsJsonAsync("/api/movimientos",
            new MovReq("Venta", 2, DateTime.UtcNow, new() { new LineaReq("A001", 6, 15m) }));

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(MovimientosController.StockInsuficiente("A001")));
    }

    [Test]
    public async Task Crear_VentaQueDejaStockEnCero_SeAcepta()
    {
        // AC-28 (RF-24): compro 10, vendo 10, el saldo queda en 0.
        await CrearArticuloAsync("A001");
        await CrearMovimientoAsync(new MovReq("Compra", 1, DateTime.UtcNow, new() { new LineaReq("A001", 10, 10m) }));

        var respuesta = await _client.PostAsJsonAsync("/api/movimientos",
            new MovReq("Venta", 2, DateTime.UtcNow, new() { new LineaReq("A001", 10, 15m) }));

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Created));
    }

    [Test]
    public async Task Crear_ConCodigoDeArticuloInexistente_Rechaza()
    {
        // RF-20: el detalle debe referir a un artículo que exista.
        var respuesta = await _client.PostAsJsonAsync("/api/movimientos",
            new MovReq("Compra", 1, DateTime.UtcNow, new() { new LineaReq("NOPE", 3, 10m) }));

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(await respuesta.Content.ReadAsStringAsync(), Does.Contain(MovimientosController.CodigoInexistente("NOPE")));
    }

    [Test]
    public async Task Modificar_UnaVenta_ExcluyeSuPropioEfectoDelSaldo()
    {
        // RF-22 + RF-24: compro 10 y vendo 10 (saldo 0). Reguardar la misma venta debe
        // aceptarse: al modificar, el saldo se mira sin las líneas viejas de este movimiento.
        await CrearArticuloAsync("A001");
        await CrearMovimientoAsync(new MovReq("Compra", 1, DateTime.UtcNow, new() { new LineaReq("A001", 10, 10m) }));
        var venta = await CrearMovimientoAsync(new MovReq("Venta", 2, DateTime.UtcNow, new() { new LineaReq("A001", 10, 15m) }));

        var respuesta = await _client.PutAsJsonAsync($"/api/movimientos/{venta.MovimientoId}",
            new MovReq("Venta", 2, DateTime.UtcNow, new() { new LineaReq("A001", 10, 15m) }));

        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private async Task CrearArticuloAsync(string codigo)
    {
        var respuesta = await _client.PostAsJsonAsync("/api/articulos",
            new { codigo, descripcion = "Artículo " + codigo, precioCosto = 10m, margen = 50m, stockMinimo = 0, puntoPedido = 0, stockIdeal = 0 });
        respuesta.EnsureSuccessStatusCode();
    }

    private async Task<MovResp> CrearMovimientoAsync(MovReq request)
    {
        var respuesta = await _client.PostAsJsonAsync("/api/movimientos", request);
        respuesta.EnsureSuccessStatusCode();
        return (await respuesta.Content.ReadFromJsonAsync<MovResp>())!;
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
    private record LineaReq(string Codigo, int Cantidad, decimal PrecioUnitario);
    private record MovReq(string Tipo, int Numero, DateTime Fecha, List<LineaReq> Detalles);
    private record LineaResp(int ArticuloId, string Codigo, string Descripcion, int Cantidad, decimal PrecioUnitario, decimal PrecioTotal);
    private record MovResp(int MovimientoId, string Tipo, int Numero, DateTime Fecha, List<LineaResp> Detalles);
}

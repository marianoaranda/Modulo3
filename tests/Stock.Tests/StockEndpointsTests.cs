using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Stock.Tests;

/// <summary>Consulta de Stock Actual contra la API real sobre SQLite en memoria (RF-25).</summary>
[TestFixture]
public class StockEndpointsTests
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
        // RF-12: la consulta también exige sesión.
        using var anonimo = _factory.CreateClient();
        var respuesta = await anonimo.GetAsync("/api/stock/actual");
        Assert.That(respuesta.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Actual_CalculaSaldoComoComprasMenosVentas()
    {
        // AC-29 (RF-25): compro 10 y vendo 3 => saldo 7.
        await CrearArticuloAsync("A001");
        await CrearMovimientoAsync(new MovReq("Compra", 1, DateTime.UtcNow, new() { new LineaReq("A001", 10, 10m) }));
        await CrearMovimientoAsync(new MovReq("Venta", 2, DateTime.UtcNow, new() { new LineaReq("A001", 3, 15m) }));

        var filas = await ConsultarAsync();

        var a001 = filas.Single(f => f.Codigo == "A001");
        Assert.That(a001.Cantidad, Is.EqualTo(7));
    }

    [Test]
    public async Task Actual_ArticuloSinMovimientos_DevuelveCantidadCero()
    {
        await CrearArticuloAsync("A001");

        var filas = await ConsultarAsync();

        Assert.That(filas.Single(f => f.Codigo == "A001").Cantidad, Is.EqualTo(0));
    }

    [Test]
    public async Task Actual_FiltraPorRangoDeCodigos()
    {
        // AC-29 (RF-25): el rango [A002, A003] excluye A001 y A004.
        foreach (var codigo in new[] { "A001", "A002", "A003", "A004" })
        {
            await CrearArticuloAsync(codigo);
        }

        var filas = await ConsultarAsync("A002", "A003");

        Assert.That(filas.Select(f => f.Codigo), Is.EqualTo(new[] { "A002", "A003" }));
    }

    private async Task<List<StockFila>> ConsultarAsync(string? desde = null, string? hasta = null)
    {
        var parametros = new List<string>();
        if (desde is not null) parametros.Add($"codigoInicial={desde}");
        if (hasta is not null) parametros.Add($"codigoFinal={hasta}");
        var url = "/api/stock/actual" + (parametros.Count > 0 ? "?" + string.Join("&", parametros) : string.Empty);

        return (await _client.GetFromJsonAsync<List<StockFila>>(url))!;
    }

    private async Task CrearArticuloAsync(string codigo)
    {
        var respuesta = await _client.PostAsJsonAsync("/api/articulos",
            new { codigo, descripcion = "Artículo " + codigo, precioCosto = 10m, margen = 50m, stockMinimo = 0, puntoPedido = 0, stockIdeal = 0 });
        respuesta.EnsureSuccessStatusCode();
    }

    private async Task CrearMovimientoAsync(MovReq request)
    {
        var respuesta = await _client.PostAsJsonAsync("/api/movimientos", request);
        respuesta.EnsureSuccessStatusCode();
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
    private record StockFila(string Codigo, string Descripcion, int Cantidad);
}

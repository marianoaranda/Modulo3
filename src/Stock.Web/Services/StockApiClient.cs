using System.Net;
using System.Net.Http.Json;

namespace Stock.Web.Services;

public record LoginApiResponse(string Token, DateTime ExpiraUtc, string NombreCompleto, string Perfil);

public interface IStockApiClient
{
    /// <summary>Devuelve los datos de sesión, o null si la API rechazó las credenciales.</summary>
    Task<LoginApiResponse?> LoginAsync(string usuario, string password, CancellationToken ct = default);

    Task<IReadOnlyList<ArticuloDto>> ListarArticulosAsync(string? descripcion = null, CancellationToken ct = default);

    /// <summary>Devuelve el artículo, o null si no existe.</summary>
    Task<ArticuloDto?> ObtenerArticuloAsync(int id, CancellationToken ct = default);

    Task<GuardarArticuloResultado> CrearArticuloAsync(ArticuloPayload articulo, CancellationToken ct = default);

    Task<GuardarArticuloResultado> ModificarArticuloAsync(int id, ArticuloPayload articulo, CancellationToken ct = default);

    Task EliminarArticuloAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<MovimientoDto>> ListarMovimientosAsync(CancellationToken ct = default);

    /// <summary>Devuelve el movimiento, o null si no existe.</summary>
    Task<MovimientoDto?> ObtenerMovimientoAsync(int id, CancellationToken ct = default);

    Task<GuardarMovimientoResultado> CrearMovimientoAsync(MovimientoPayload movimiento, CancellationToken ct = default);

    Task<GuardarMovimientoResultado> ModificarMovimientoAsync(int id, MovimientoPayload movimiento, CancellationToken ct = default);

    Task EliminarMovimientoAsync(int id, CancellationToken ct = default);
}

public class StockApiClient : IStockApiClient
{
    public const string HttpClientName = "StockApi";

    private readonly HttpClient _http;

    public StockApiClient(IHttpClientFactory factory) => _http = factory.CreateClient(HttpClientName);

    public async Task<LoginApiResponse?> LoginAsync(string usuario, string password, CancellationToken ct = default)
    {
        var respuesta = await _http.PostAsJsonAsync("api/auth/login",
            new { Usuario = usuario, Password = password }, ct);

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<ArticuloDto>> ListarArticulosAsync(string? descripcion = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(descripcion)
            ? "api/articulos"
            : $"api/articulos?descripcion={Uri.EscapeDataString(descripcion)}";

        var articulos = await _http.GetFromJsonAsync<List<ArticuloDto>>(url, ct);
        return articulos ?? new List<ArticuloDto>();
    }

    public async Task<ArticuloDto?> ObtenerArticuloAsync(int id, CancellationToken ct = default)
    {
        var respuesta = await _http.GetAsync($"api/articulos/{id}", ct);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<ArticuloDto>(cancellationToken: ct);
    }

    public async Task<GuardarArticuloResultado> CrearArticuloAsync(ArticuloPayload articulo, CancellationToken ct = default)
    {
        var respuesta = await _http.PostAsJsonAsync("api/articulos", articulo, ct);
        return await InterpretarGuardado(respuesta, ct);
    }

    public async Task<GuardarArticuloResultado> ModificarArticuloAsync(int id, ArticuloPayload articulo, CancellationToken ct = default)
    {
        var respuesta = await _http.PutAsJsonAsync($"api/articulos/{id}", articulo, ct);
        return await InterpretarGuardado(respuesta, ct);
    }

    public async Task EliminarArticuloAsync(int id, CancellationToken ct = default)
    {
        var respuesta = await _http.DeleteAsync($"api/articulos/{id}", ct);
        respuesta.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<MovimientoDto>> ListarMovimientosAsync(CancellationToken ct = default)
    {
        var movimientos = await _http.GetFromJsonAsync<List<MovimientoDto>>("api/movimientos", ct);
        return movimientos ?? new List<MovimientoDto>();
    }

    public async Task<MovimientoDto?> ObtenerMovimientoAsync(int id, CancellationToken ct = default)
    {
        var respuesta = await _http.GetAsync($"api/movimientos/{id}", ct);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<MovimientoDto>(cancellationToken: ct);
    }

    public async Task<GuardarMovimientoResultado> CrearMovimientoAsync(MovimientoPayload movimiento, CancellationToken ct = default)
    {
        var respuesta = await _http.PostAsJsonAsync("api/movimientos", movimiento, ct);
        if (await EsRechazoDeValidacion(respuesta, ct) is { } errores)
        {
            return GuardarMovimientoResultado.Invalido(errores);
        }

        respuesta.EnsureSuccessStatusCode();
        return GuardarMovimientoResultado.Ok((await respuesta.Content.ReadFromJsonAsync<MovimientoDto>(cancellationToken: ct))!);
    }

    public async Task<GuardarMovimientoResultado> ModificarMovimientoAsync(int id, MovimientoPayload movimiento, CancellationToken ct = default)
    {
        var respuesta = await _http.PutAsJsonAsync($"api/movimientos/{id}", movimiento, ct);
        if (await EsRechazoDeValidacion(respuesta, ct) is { } errores)
        {
            return GuardarMovimientoResultado.Invalido(errores);
        }

        respuesta.EnsureSuccessStatusCode();
        return GuardarMovimientoResultado.Ok((await respuesta.Content.ReadFromJsonAsync<MovimientoDto>(cancellationToken: ct))!);
    }

    public async Task EliminarMovimientoAsync(int id, CancellationToken ct = default)
    {
        var respuesta = await _http.DeleteAsync($"api/movimientos/{id}", ct);
        respuesta.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Traduce la respuesta de guardar: 400 lo tomamos como rechazo por reglas de
    /// negocio y extraemos los errores del ProblemDetails; cualquier otro fallo se
    /// propaga como excepción (no es algo que el formulario pueda corregir).
    /// </summary>
    private static async Task<GuardarArticuloResultado> InterpretarGuardado(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (await EsRechazoDeValidacion(respuesta, ct) is { } errores)
        {
            return GuardarArticuloResultado.Invalido(errores);
        }

        respuesta.EnsureSuccessStatusCode();
        var articulo = await respuesta.Content.ReadFromJsonAsync<ArticuloDto>(cancellationToken: ct);
        return GuardarArticuloResultado.Ok(articulo!);
    }

    /// <summary>Si la respuesta es un 400 de validación, devuelve los errores por campo; si no, null.</summary>
    private static async Task<IReadOnlyDictionary<string, string[]>?> EsRechazoDeValidacion(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.StatusCode != HttpStatusCode.BadRequest)
        {
            return null;
        }

        var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemResponse>(cancellationToken: ct);
        return problema?.Errors ?? new Dictionary<string, string[]>();
    }

    /// <summary>Forma mínima del ProblemDetails de validación que emite ASP.NET.</summary>
    private record ValidationProblemResponse(IReadOnlyDictionary<string, string[]> Errors);
}

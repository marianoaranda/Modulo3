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

    /// <summary>
    /// Traduce la respuesta de guardar: 400 lo tomamos como rechazo por reglas de
    /// negocio y extraemos los errores del ProblemDetails; cualquier otro fallo se
    /// propaga como excepción (no es algo que el formulario pueda corregir).
    /// </summary>
    private static async Task<GuardarArticuloResultado> InterpretarGuardado(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.StatusCode == HttpStatusCode.BadRequest)
        {
            var problema = await respuesta.Content.ReadFromJsonAsync<ValidationProblemResponse>(cancellationToken: ct);
            return GuardarArticuloResultado.Invalido(problema?.Errors ?? new Dictionary<string, string[]>());
        }

        respuesta.EnsureSuccessStatusCode();
        var articulo = await respuesta.Content.ReadFromJsonAsync<ArticuloDto>(cancellationToken: ct);
        return GuardarArticuloResultado.Ok(articulo!);
    }

    /// <summary>Forma mínima del ProblemDetails de validación que emite ASP.NET.</summary>
    private record ValidationProblemResponse(IReadOnlyDictionary<string, string[]> Errors);
}

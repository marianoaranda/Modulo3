using System.Net;
using System.Net.Http.Json;

namespace Stock.Web.Services;

public record LoginApiResponse(string Token, DateTime ExpiraUtc, string NombreCompleto, string Perfil);

public interface IStockApiClient
{
    /// <summary>Devuelve los datos de sesión, o null si la API rechazó las credenciales.</summary>
    Task<LoginApiResponse?> LoginAsync(string usuario, string password, CancellationToken ct = default);
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
}

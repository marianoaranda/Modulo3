namespace Stock.Web.Services;

/// <summary>Artículo tal como lo devuelve la API (incluye el Precio de Venta calculado).</summary>
public record ArticuloDto(
    int ArticuloId,
    string Codigo,
    string Descripcion,
    decimal PrecioCosto,
    decimal Margen,
    decimal PrecioVenta,
    int StockMinimo,
    int PuntoPedido,
    int StockIdeal);

/// <summary>Cuerpo de alta/modificación que espera la API (sin Precio de Venta: se calcula).</summary>
public record ArticuloPayload(
    string Codigo,
    string Descripcion,
    decimal PrecioCosto,
    decimal Margen,
    int StockMinimo,
    int PuntoPedido,
    int StockIdeal);

/// <summary>
/// Resultado de guardar un artículo. Si la API rechazó por reglas de negocio
/// (RF-17, RF-18, RF-19), Errores trae los mensajes agrupados por campo, con la
/// misma clave que usa la API ("" para los generales, "Codigo" para el duplicado).
/// </summary>
public record GuardarArticuloResultado(
    bool Exito,
    ArticuloDto? Articulo,
    IReadOnlyDictionary<string, string[]> Errores)
{
    private static readonly IReadOnlyDictionary<string, string[]> SinErrores =
        new Dictionary<string, string[]>();

    public static GuardarArticuloResultado Ok(ArticuloDto articulo) => new(true, articulo, SinErrores);

    public static GuardarArticuloResultado Invalido(IReadOnlyDictionary<string, string[]> errores) =>
        new(false, null, errores);
}

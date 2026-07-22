namespace Stock.Web.Services;

/// <summary>Tipo de movimiento para el formulario del sitio. En el transporte viaja como texto.</summary>
public enum TipoMovimiento
{
    Compra,
    Venta
}

/// <summary>Movimiento tal como lo devuelve la API. El Tipo llega como texto ("Compra"/"Venta").</summary>
public record MovimientoDto(
    int MovimientoId,
    string Tipo,
    int Numero,
    DateTime Fecha,
    IReadOnlyList<MovimientoDetalleDto> Detalles);

public record MovimientoDetalleDto(
    int ArticuloId,
    string Codigo,
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal PrecioTotal);

/// <summary>Próximo número correlativo sugerido por la API para un alta.</summary>
public record SiguienteNumeroDto(int Numero);

/// <summary>Cuerpo de alta/modificación. El Precio Total no se manda: lo calcula la API.</summary>
public record MovimientoPayload(
    string Tipo,
    int Numero,
    DateTime Fecha,
    IReadOnlyList<MovimientoDetallePayload> Detalles);

public record MovimientoDetallePayload(
    string Codigo,
    int Cantidad,
    decimal PrecioUnitario);

/// <summary>Resultado de guardar un movimiento; ante un 400, Errores trae los mensajes de la API por campo.</summary>
public record GuardarMovimientoResultado(
    bool Exito,
    MovimientoDto? Movimiento,
    IReadOnlyDictionary<string, string[]> Errores)
{
    private static readonly IReadOnlyDictionary<string, string[]> SinErrores = new Dictionary<string, string[]>();

    public static GuardarMovimientoResultado Ok(MovimientoDto movimiento) => new(true, movimiento, SinErrores);

    public static GuardarMovimientoResultado Invalido(IReadOnlyDictionary<string, string[]> errores) =>
        new(false, null, errores);
}

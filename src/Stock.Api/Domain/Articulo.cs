namespace Stock.Api.Domain;

/// <summary>Artículo del catálogo (RF-13 a RF-19).</summary>
public class Articulo
{
    public int ArticuloId { get; set; }

    /// <summary>Código de negocio, único entre artículos (RF-17).</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public decimal PrecioCosto { get; set; }

    /// <summary>Margen en porcentaje (ej: 50 = 50%).</summary>
    public decimal Margen { get; set; }

    /// <summary>
    /// Niveles de stock del artículo. Son cantidades enteras, en línea con la
    /// cantidad entera de los movimientos (RF-23). Restricción: Mínimo ≤ Punto ≤ Ideal (RF-19).
    /// </summary>
    public int StockMinimo { get; set; }
    public int PuntoPedido { get; set; }
    public int StockIdeal { get; set; }

    /// <summary>
    /// Precio de Venta calculado a partir de Costo y Margen (RF-16). No se persiste:
    /// se deriva siempre para que no pueda quedar desincronizado del costo o el margen.
    /// </summary>
    public decimal PrecioVenta => CalcularPrecioVenta(PrecioCosto, Margen);

    /// <summary>Precio de Venta = Precio de Costo × (1 + Margen / 100) (RF-16).</summary>
    public static decimal CalcularPrecioVenta(decimal precioCosto, decimal margen)
        => precioCosto * (1 + margen / 100m);
}

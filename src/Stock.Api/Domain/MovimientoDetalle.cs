namespace Stock.Api.Domain;

/// <summary>Línea de detalle de un movimiento (RF-20). Referencia al artículo por su clave.</summary>
public class MovimientoDetalle
{
    public int MovimientoDetalleId { get; set; }

    public int MovimientoId { get; set; }
    public Movimiento? Movimiento { get; set; }

    public int ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    /// <summary>Cantidad; debe ser un entero mayor que 0 (RF-23).</summary>
    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }
    public decimal PrecioTotal { get; set; }
}

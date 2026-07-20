namespace Stock.Api.Domain;

/// <summary>
/// Tipo de movimiento. El signo con el que impacta en el stock lo define este tipo:
/// la compra suma y la venta resta (RF-25).
/// </summary>
public enum TipoMovimiento
{
    Compra = 1,
    Venta = 2
}

/// <summary>Encabezado de un movimiento de compra o venta (RF-20 a RF-22).</summary>
public class Movimiento
{
    public int MovimientoId { get; set; }
    public TipoMovimiento Tipo { get; set; }

    /// <summary>Número de comprobante informado por el usuario (RF-20).</summary>
    public int Numero { get; set; }

    public DateTime Fecha { get; set; }

    public ICollection<MovimientoDetalle> Detalles { get; set; } = new List<MovimientoDetalle>();
}

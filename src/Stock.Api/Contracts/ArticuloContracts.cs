using System.ComponentModel.DataAnnotations;

namespace Stock.Api.Contracts;

/// <summary>Datos de alta/modificación de un artículo (RF-13, RF-15). El Precio de Venta no entra: se calcula (RF-16).</summary>
public record ArticuloRequest
{
    [Required]
    [MaxLength(50)]
    public string Codigo { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Descripcion { get; init; } = string.Empty;

    public decimal PrecioCosto { get; init; }
    public decimal Margen { get; init; }
    public int StockMinimo { get; init; }
    public int PuntoPedido { get; init; }
    public int StockIdeal { get; init; }
}

/// <summary>Primer y último código del catálogo, para sugerir el rango de la Consulta de Stock Actual.</summary>
public record RangoCodigosResponse(string? Primero, string? Ultimo);

public record ArticuloResponse(
    int ArticuloId,
    string Codigo,
    string Descripcion,
    decimal PrecioCosto,
    decimal Margen,
    decimal PrecioVenta,
    int StockMinimo,
    int PuntoPedido,
    int StockIdeal);

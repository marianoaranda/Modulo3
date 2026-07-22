using System.ComponentModel.DataAnnotations;
using Stock.Api.Domain;

namespace Stock.Api.Contracts;

/// <summary>Alta/modificación de un movimiento (RF-20, RF-22). El detalle referencia al artículo por su Código.</summary>
public record MovimientoRequest
{
    public TipoMovimiento Tipo { get; init; }
    public int Numero { get; init; }
    public DateTime Fecha { get; init; }

    [Required]
    public List<MovimientoDetalleRequest> Detalles { get; init; } = new();
}

/// <summary>Línea de detalle. El Precio Total no se informa: lo calcula el servidor (Cantidad × Precio Unitario).</summary>
public record MovimientoDetalleRequest
{
    [Required]
    public string Codigo { get; init; } = string.Empty;

    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
}

/// <summary>Próximo número correlativo sugerido para el alta de un movimiento.</summary>
public record SiguienteNumeroResponse(int Numero);

public record MovimientoResponse(
    int MovimientoId,
    TipoMovimiento Tipo,
    int Numero,
    DateTime Fecha,
    IReadOnlyList<MovimientoDetalleResponse> Detalles);

public record MovimientoDetalleResponse(
    int ArticuloId,
    string Codigo,
    string Descripcion,
    int Cantidad,
    decimal PrecioUnitario,
    decimal PrecioTotal);

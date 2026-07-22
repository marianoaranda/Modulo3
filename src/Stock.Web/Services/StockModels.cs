using System.ComponentModel.DataAnnotations;

namespace Stock.Web.Services;

/// <summary>Fila de la Consulta de Stock Actual (RF-25): saldo por artículo.</summary>
public record StockActualDto(string Codigo, string Descripcion, int Cantidad);

/// <summary>Modo de reposición de la consulta Generar Pedido (RF-26). Viaja como texto hacia la API.</summary>
public enum ModoPedido
{
    [Display(Name = "Hasta Stock Mínimo")]
    HastaStockMinimo,

    [Display(Name = "Hasta Punto de Pedido")]
    HastaPuntoPedido,

    [Display(Name = "Hasta Stock Ideal")]
    HastaStockIdeal
}

/// <summary>Fila de la consulta Generar Pedido (RF-26): cantidad a pedir por artículo.</summary>
public record PedidoDto(string Codigo, string Descripcion, int CantidadAPedir);

/// <summary>Primer y último código del catálogo, para sugerir el rango de la consulta.</summary>
public record RangoCodigosDto(string? Primero, string? Ultimo);

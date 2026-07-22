namespace Stock.Web.Services;

/// <summary>Fila de la Consulta de Stock Actual (RF-25): saldo por artículo.</summary>
public record StockActualDto(string Codigo, string Descripcion, int Cantidad);

/// <summary>Primer y último código del catálogo, para sugerir el rango de la consulta.</summary>
public record RangoCodigosDto(string? Primero, string? Ultimo);

namespace Stock.Api.Contracts;

/// <summary>Una fila de la Consulta de Stock Actual (RF-25): saldo por artículo.</summary>
public record StockActualResponse(string Codigo, string Descripcion, int Cantidad);

/// <summary>Una fila de la consulta Generar Pedido (RF-26): cantidad a pedir por artículo.</summary>
public record PedidoResponse(string Codigo, string Descripcion, int CantidadAPedir);

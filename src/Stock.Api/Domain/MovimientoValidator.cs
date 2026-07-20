namespace Stock.Api.Domain;

/// <summary>
/// Reglas de negocio de un movimiento. Las estructurales (RF-23) dependen sólo del
/// propio movimiento; la de stock de venta (RF-24) necesita el saldo actual, que se
/// le pasa ya calculado para que la regla siga siendo pura y testeable sin base.
/// </summary>
public static class MovimientoValidator
{
    public const string SinDetalle = "El movimiento debe tener al menos una línea de detalle.";
    public const string CantidadInvalida = "La cantidad de cada línea debe ser un número entero mayor que 0.";

    /// <summary>Validaciones estructurales del movimiento (RF-23).</summary>
    public static IReadOnlyList<string> Validar(Movimiento movimiento)
    {
        var errores = new List<string>();

        if (movimiento.Detalles.Count == 0)
        {
            errores.Add(SinDetalle);
        }

        // RF-23: cantidad entera mayor que 0. Que sea entero ya lo garantiza el tipo int
        // en el borde de la API; acá sólo queda cubrir el > 0.
        if (movimiento.Detalles.Any(d => d.Cantidad <= 0))
        {
            errores.Add(CantidadInvalida);
        }

        return errores;
    }

    /// <summary>
    /// RF-24: una venta no puede dejar el stock de ningún artículo por debajo de 0.
    /// Devuelve los ArticuloId que quedarían negativos; vacío si la venta es válida.
    /// </summary>
    /// <param name="cantidadVendidaPorArticulo">Cantidad total vendida en el movimiento, por artículo.</param>
    /// <param name="saldoActualPorArticulo">Saldo actual de cada artículo, sin contar este movimiento.</param>
    public static IReadOnlyList<int> ArticulosConStockInsuficiente(
        IReadOnlyDictionary<int, int> cantidadVendidaPorArticulo,
        IReadOnlyDictionary<int, int> saldoActualPorArticulo)
    {
        var faltantes = new List<int>();

        foreach (var (articuloId, vendida) in cantidadVendidaPorArticulo)
        {
            saldoActualPorArticulo.TryGetValue(articuloId, out var saldo);
            if (saldo - vendida < 0)
            {
                faltantes.Add(articuloId);
            }
        }

        return faltantes;
    }
}

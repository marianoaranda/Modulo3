namespace Stock.Api.Domain;

/// <summary>
/// Reglas de negocio intrínsecas de un artículo, las que dependen sólo de sus
/// propios campos (RF-18, RF-19). La unicidad del código (RF-17) no vive acá:
/// necesita conocer al resto de los artículos, así que se valida contra la base.
/// </summary>
public static class ArticuloValidator
{
    public const string ValoresNegativos =
        "Precio de Costo, Margen, Stock Mínimo, Punto de Pedido y Stock Ideal no pueden ser negativos.";

    public const string OrdenDeStock =
        "Debe cumplirse Stock Mínimo ≤ Punto de Pedido ≤ Stock Ideal.";

    /// <summary>Devuelve los mensajes de error del artículo; vacío si es válido.</summary>
    public static IReadOnlyList<string> Validar(Articulo articulo)
    {
        var errores = new List<string>();

        // RF-18: ningún importe ni nivel de stock puede ser negativo.
        if (articulo.PrecioCosto < 0 || articulo.Margen < 0 ||
            articulo.StockMinimo < 0 || articulo.PuntoPedido < 0 || articulo.StockIdeal < 0)
        {
            errores.Add(ValoresNegativos);
        }

        // RF-19: los niveles de stock deben estar ordenados.
        if (!(articulo.StockMinimo <= articulo.PuntoPedido && articulo.PuntoPedido <= articulo.StockIdeal))
        {
            errores.Add(OrdenDeStock);
        }

        return errores;
    }
}

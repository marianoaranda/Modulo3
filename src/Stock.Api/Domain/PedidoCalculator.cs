namespace Stock.Api.Domain;

/// <summary>Nivel hasta el que se repone cada artículo al generar el pedido (RF-26).</summary>
public enum ModoPedido
{
    HastaStockMinimo,
    HastaPuntoPedido,
    HastaStockIdeal
}

/// <summary>
/// Cálculo de la Cantidad a Pedir por artículo (RF-26 / AC-31 a AC-36). Es lógica pura:
/// recibe el saldo y los niveles ya cargados, para poder testear las 6 combinaciones sin base.
/// </summary>
public static class PedidoCalculator
{
    /// <summary>Nivel objetivo de reposición según el modo elegido.</summary>
    public static int NivelObjetivo(int stockMinimo, int puntoPedido, int stockIdeal, ModoPedido modo) => modo switch
    {
        ModoPedido.HastaStockMinimo => stockMinimo,
        ModoPedido.HastaPuntoPedido => puntoPedido,
        ModoPedido.HastaStockIdeal => stockIdeal,
        _ => stockMinimo
    };

    /// <summary>
    /// Cantidad a pedir para un artículo, o null si no entra en el pedido.
    /// Con soloBajoMinimo se incluyen únicamente los que están por debajo del mínimo (AC-34 a AC-36);
    /// sin él, entran todos con MAX(0, objetivo − saldo) (AC-31 a AC-33).
    /// </summary>
    public static int? CantidadAPedir(int saldo, int stockMinimo, int nivelObjetivo, bool soloBajoMinimo)
    {
        if (soloBajoMinimo && saldo >= stockMinimo)
        {
            return null;
        }

        var cantidad = nivelObjetivo - saldo;
        // Bajo mínimo el objetivo siempre supera al saldo (mínimo ≤ punto ≤ ideal, RF-19),
        // así que la cantidad ya es positiva; sin el filtro, acotamos a 0.
        return soloBajoMinimo ? cantidad : Math.Max(0, cantidad);
    }
}

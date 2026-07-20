using Microsoft.EntityFrameworkCore;
using Stock.Api.Data;
using Stock.Api.Domain;

namespace Stock.Api.Services;

/// <summary>
/// Calcula el saldo de stock de los artículos a partir de los movimientos
/// (las compras suman y las ventas restan, RF-25). Lo usan la validación de
/// ventas (RF-24) y, más adelante, las consultas de stock y de pedido (RF-25, RF-26).
/// </summary>
public interface IStockService
{
    /// <summary>
    /// Devuelve el saldo actual por artículo. Un artículo sin movimientos no aparece
    /// en el diccionario: hay que interpretarlo como saldo 0.
    /// </summary>
    /// <param name="articuloIds">Si se informa, acota el cálculo a esos artículos.</param>
    /// <param name="excluirMovimientoId">
    /// Movimiento a excluir del saldo. Se usa al modificar (RF-22): el saldo debe
    /// mirarse sin las líneas viejas del propio movimiento que se está por reemplazar.
    /// </param>
    Task<IReadOnlyDictionary<int, int>> ObtenerSaldosAsync(
        IReadOnlyCollection<int>? articuloIds = null,
        int? excluirMovimientoId = null,
        CancellationToken ct = default);
}

public class StockService : IStockService
{
    private readonly AppDbContext _db;

    public StockService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<int, int>> ObtenerSaldosAsync(
        IReadOnlyCollection<int>? articuloIds = null,
        int? excluirMovimientoId = null,
        CancellationToken ct = default)
    {
        var detalles = _db.MovimientoDetalles.AsQueryable();

        if (excluirMovimientoId is not null)
        {
            detalles = detalles.Where(d => d.MovimientoId != excluirMovimientoId);
        }

        if (articuloIds is not null)
        {
            detalles = detalles.Where(d => articuloIds.Contains(d.ArticuloId));
        }

        // El signo lo define el tipo del movimiento; lo proyectamos antes de agrupar
        // para que la suma se traduzca a SQL sin complicaciones.
        return await detalles
            .Select(d => new
            {
                d.ArticuloId,
                Delta = d.Movimiento!.Tipo == TipoMovimiento.Compra ? d.Cantidad : -d.Cantidad
            })
            .GroupBy(x => x.ArticuloId)
            .Select(g => new { ArticuloId = g.Key, Saldo = g.Sum(x => x.Delta) })
            .ToDictionaryAsync(x => x.ArticuloId, x => x.Saldo, ct);
    }
}

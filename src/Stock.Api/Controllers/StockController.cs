using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock.Api.Contracts;
using Stock.Api.Data;
using Stock.Api.Services;

namespace Stock.Api.Controllers;

/// <summary>Consultas de stock (RF-25). Protegido por JWT como todo el resto (RF-12).</summary>
[ApiController]
[Route("api/stock")]
public class StockController : ControllerBase
{
    /// <summary>Tope de artículos por consulta, para acotar el volumen (RNF-04, riesgo del PRD).</summary>
    private const int TopArticulos = 10000;

    private readonly AppDbContext _db;
    private readonly IStockService _stock;

    public StockController(AppDbContext db, IStockService stock)
    {
        _db = db;
        _stock = stock;
    }

    /// <summary>
    /// Consulta de Stock Actual (RF-25 / AC-29): saldo por artículo dentro de un rango de códigos.
    /// La cantidad sale del saldo de movimientos (compras suman, ventas restan); un artículo sin
    /// movimientos vale 0. Los dos parámetros de rango son opcionales.
    /// </summary>
    [HttpGet("actual")]
    public async Task<ActionResult<IEnumerable<StockActualResponse>>> Actual(
        [FromQuery] string? codigoInicial = null,
        [FromQuery] string? codigoFinal = null,
        CancellationToken ct = default)
    {
        var consulta = _db.Articulos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(codigoInicial))
        {
            var desde = codigoInicial.Trim();
            consulta = consulta.Where(a => string.Compare(a.Codigo, desde) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(codigoFinal))
        {
            var hasta = codigoFinal.Trim();
            consulta = consulta.Where(a => string.Compare(a.Codigo, hasta) <= 0);
        }

        var articulos = await consulta
            .OrderBy(a => a.Codigo)
            .Take(TopArticulos)
            .Select(a => new { a.ArticuloId, a.Codigo, a.Descripcion })
            .ToListAsync(ct);

        // Saldo de todos los artículos en una sola agregación; el diccionario no trae
        // a los que no tienen movimientos, que se interpretan como saldo 0.
        var saldos = await _stock.ObtenerSaldosAsync(ct: ct);

        var filas = articulos.Select(a =>
            new StockActualResponse(a.Codigo, a.Descripcion, saldos.GetValueOrDefault(a.ArticuloId)));

        return Ok(filas);
    }
}

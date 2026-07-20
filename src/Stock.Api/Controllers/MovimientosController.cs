using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock.Api.Contracts;
using Stock.Api.Data;
using Stock.Api.Domain;
using Stock.Api.Services;

namespace Stock.Api.Controllers;

/// <summary>ABM de movimientos de compra y venta (RF-20 a RF-24). Protegido por JWT (RF-12).</summary>
[ApiController]
[Route("api/movimientos")]
public class MovimientosController : ControllerBase
{
    private const int TopMovimientos = 10000;

    public static string CodigoInexistente(string codigo) => $"No existe un artículo con el código {codigo}.";
    public static string StockInsuficiente(string codigo) =>
        $"El movimiento dejaría el stock del artículo {codigo} por debajo de 0.";

    private readonly AppDbContext _db;
    private readonly IStockService _stock;

    public MovimientosController(AppDbContext db, IStockService stock)
    {
        _db = db;
        _stock = stock;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MovimientoResponse>>> Listar(CancellationToken ct)
    {
        var movimientos = await ConDetalles(_db.Movimientos.AsNoTracking())
            .OrderByDescending(m => m.MovimientoId)
            .Take(TopMovimientos)
            .ToListAsync(ct);

        return Ok(movimientos.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovimientoResponse>> ObtenerPorId(int id, CancellationToken ct)
    {
        var movimiento = await ConDetalles(_db.Movimientos.AsNoTracking())
            .SingleOrDefaultAsync(m => m.MovimientoId == id, ct);

        return movimiento is null ? NotFound() : Ok(ToResponse(movimiento));
    }

    /// <summary>Alta de movimiento (RF-20).</summary>
    [HttpPost]
    public async Task<ActionResult<MovimientoResponse>> Crear(MovimientoRequest request, CancellationToken ct)
    {
        var movimiento = new Movimiento();
        if (!await IntentarArmar(request, movimiento, excluirMovimientoId: null, ct))
        {
            return ValidationProblem(ModelState);
        }

        _db.Movimientos.Add(movimiento);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = movimiento.MovimientoId }, ToResponse(movimiento));
    }

    /// <summary>Modificación de movimiento, encabezado y detalle (RF-22).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MovimientoResponse>> Modificar(int id, MovimientoRequest request, CancellationToken ct)
    {
        var movimiento = await _db.Movimientos
            .Include(m => m.Detalles)
            .SingleOrDefaultAsync(m => m.MovimientoId == id, ct);
        if (movimiento is null)
        {
            return NotFound();
        }

        // Al modificar, el saldo para RF-24 se mira sin las líneas viejas de este mismo movimiento.
        if (!await IntentarArmar(request, movimiento, excluirMovimientoId: id, ct))
        {
            return ValidationProblem(ModelState);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(ToResponse(movimiento));
    }

    /// <summary>Baja de movimiento; el detalle se elimina en cascada (RF-21).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var movimiento = await _db.Movimientos.SingleOrDefaultAsync(m => m.MovimientoId == id, ct);
        if (movimiento is null)
        {
            return NotFound();
        }

        _db.Movimientos.Remove(movimiento);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Vuelca el request sobre el movimiento (nuevo o existente) resolviendo los
    /// códigos de artículo (RF-20) y corriendo las validaciones RF-23 y RF-24.
    /// Deja los errores en ModelState; devuelve true si el movimiento quedó válido.
    /// </summary>
    private async Task<bool> IntentarArmar(MovimientoRequest request, Movimiento movimiento, int? excluirMovimientoId, CancellationToken ct)
    {
        movimiento.Tipo = request.Tipo;
        movimiento.Numero = request.Numero;
        movimiento.Fecha = request.Fecha;

        var codigos = request.Detalles
            .Select(d => d.Codigo.Trim())
            .Where(c => c.Length > 0)
            .Distinct()
            .ToList();
        var articulos = await _db.Articulos
            .Where(a => codigos.Contains(a.Codigo))
            .ToDictionaryAsync(a => a.Codigo, ct);

        // Reemplazamos el detalle completo; en un movimiento existente esto marca las
        // líneas viejas para borrado (FK requerida + cascada).
        movimiento.Detalles.Clear();
        foreach (var linea in request.Detalles)
        {
            var codigo = linea.Codigo.Trim();
            if (!articulos.TryGetValue(codigo, out var articulo))
            {
                ModelState.AddModelError(nameof(MovimientoRequest.Detalles), CodigoInexistente(codigo));
                continue;
            }

            movimiento.Detalles.Add(new MovimientoDetalle
            {
                ArticuloId = articulo.ArticuloId,
                Articulo = articulo,
                Cantidad = linea.Cantidad,
                PrecioUnitario = linea.PrecioUnitario,
                PrecioTotal = linea.Cantidad * linea.PrecioUnitario
            });
        }

        // RF-23: estructura y cantidades.
        foreach (var error in MovimientoValidator.Validar(movimiento))
        {
            ModelState.AddModelError(string.Empty, error);
        }

        // RF-24: sólo tiene sentido para ventas y una vez que el resto está en orden
        // (con códigos o cantidades inválidas, el chequeo de stock no aportaría nada).
        if (movimiento.Tipo == TipoMovimiento.Venta && ModelState.IsValid)
        {
            var vendidaPorArticulo = movimiento.Detalles
                .GroupBy(d => d.ArticuloId)
                .ToDictionary(g => g.Key, g => g.Sum(d => d.Cantidad));

            var saldos = await _stock.ObtenerSaldosAsync(vendidaPorArticulo.Keys.ToList(), excluirMovimientoId, ct);

            foreach (var articuloId in MovimientoValidator.ArticulosConStockInsuficiente(vendidaPorArticulo, saldos))
            {
                var codigo = movimiento.Detalles.First(d => d.ArticuloId == articuloId).Articulo!.Codigo;
                ModelState.AddModelError(string.Empty, StockInsuficiente(codigo));
            }
        }

        return ModelState.IsValid;
    }

    private static IQueryable<Movimiento> ConDetalles(IQueryable<Movimiento> query) =>
        query.Include(m => m.Detalles).ThenInclude(d => d.Articulo);

    private static MovimientoResponse ToResponse(Movimiento m) => new(
        m.MovimientoId,
        m.Tipo,
        m.Numero,
        m.Fecha,
        m.Detalles.Select(d => new MovimientoDetalleResponse(
            d.ArticuloId,
            d.Articulo?.Codigo ?? string.Empty,
            d.Articulo?.Descripcion ?? string.Empty,
            d.Cantidad,
            d.PrecioUnitario,
            d.PrecioTotal)).ToList());
}

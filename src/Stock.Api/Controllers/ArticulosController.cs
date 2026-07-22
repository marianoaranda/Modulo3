using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stock.Api.Contracts;
using Stock.Api.Data;
using Stock.Api.Domain;

namespace Stock.Api.Controllers;

/// <summary>ABM de artículos (RF-13 a RF-19). Protegido por JWT como todo el resto (RF-12).</summary>
[ApiController]
[Route("api/articulos")]
public class ArticulosController : ControllerBase
{
    /// <summary>Tope de artículos por consulta, para acotar el volumen (ver riesgo del PRD).</summary>
    private const int TopArticulos = 10000;

    public const string CodigoDuplicado = "Ya existe un artículo con ese Código.";

    private readonly AppDbContext _db;

    public ArticulosController(AppDbContext db) => _db = db;

    /// <summary>Listado para la grilla, con filtro opcional por descripción y TOP para acotar volumen.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ArticuloResponse>>> Listar([FromQuery] string? descripcion = null)
    {
        var consulta = _db.Articulos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(descripcion))
        {
            consulta = consulta.Where(a => EF.Functions.Like(a.Descripcion, $"%{descripcion}%"));
        }

        var articulos = await consulta
            .OrderBy(a => a.Codigo)
            .Take(TopArticulos)
            .ToListAsync();

        return Ok(articulos.Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ArticuloResponse>> ObtenerPorId(int id)
    {
        var articulo = await _db.Articulos.AsNoTracking().SingleOrDefaultAsync(a => a.ArticuloId == id);
        return articulo is null ? NotFound() : Ok(ToResponse(articulo));
    }

    /// <summary>Primer y último código del catálogo, para sugerir el rango de la Consulta de Stock Actual.</summary>
    [HttpGet("rango-codigos")]
    public async Task<ActionResult<RangoCodigosResponse>> RangoCodigos(CancellationToken ct)
    {
        var primero = await _db.Articulos.AsNoTracking()
            .OrderBy(a => a.Codigo).Select(a => a.Codigo).FirstOrDefaultAsync(ct);
        var ultimo = await _db.Articulos.AsNoTracking()
            .OrderByDescending(a => a.Codigo).Select(a => a.Codigo).FirstOrDefaultAsync(ct);

        return Ok(new RangoCodigosResponse(primero, ultimo));
    }

    /// <summary>
    /// Búsqueda puntual por Código para la carga de movimientos: el formulario la usa
    /// para mostrar la descripción y sugerir el precio (costo o venta) al tipear el código.
    /// </summary>
    [HttpGet("por-codigo/{codigo}")]
    public async Task<ActionResult<ArticuloResponse>> ObtenerPorCodigo(string codigo)
    {
        var buscado = codigo.Trim();
        var articulo = await _db.Articulos.AsNoTracking().SingleOrDefaultAsync(a => a.Codigo == buscado);
        return articulo is null ? NotFound() : Ok(ToResponse(articulo));
    }

    /// <summary>Alta de artículo (RF-13).</summary>
    [HttpPost]
    public async Task<ActionResult<ArticuloResponse>> Crear(ArticuloRequest request)
    {
        var articulo = new Articulo();
        VolcarDatos(request, articulo);

        if (!await EsValido(articulo))
        {
            return ValidationProblem(ModelState);
        }

        _db.Articulos.Add(articulo);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObtenerPorId), new { id = articulo.ArticuloId }, ToResponse(articulo));
    }

    /// <summary>Modificación de artículo (RF-15).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ArticuloResponse>> Modificar(int id, ArticuloRequest request)
    {
        var articulo = await _db.Articulos.SingleOrDefaultAsync(a => a.ArticuloId == id);
        if (articulo is null)
        {
            return NotFound();
        }

        VolcarDatos(request, articulo);

        if (!await EsValido(articulo))
        {
            return ValidationProblem(ModelState);
        }

        await _db.SaveChangesAsync();
        return Ok(ToResponse(articulo));
    }

    /// <summary>Baja de artículo (RF-14).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var articulo = await _db.Articulos.SingleOrDefaultAsync(a => a.ArticuloId == id);
        if (articulo is null)
        {
            return NotFound();
        }

        _db.Articulos.Remove(articulo);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Corre las reglas de negocio y deja los errores en ModelState. Cubre las
    /// intrínsecas (RF-18, RF-19) y la unicidad del código contra la base (RF-17).
    /// </summary>
    private async Task<bool> EsValido(Articulo articulo)
    {
        foreach (var error in ArticuloValidator.Validar(articulo))
        {
            ModelState.AddModelError(string.Empty, error);
        }

        // RF-17: el código no puede repetirse. Se excluye el propio artículo para no chocar consigo mismo al modificar.
        var codigoEnUso = await _db.Articulos
            .AnyAsync(a => a.Codigo == articulo.Codigo && a.ArticuloId != articulo.ArticuloId);
        if (codigoEnUso)
        {
            ModelState.AddModelError(nameof(ArticuloRequest.Codigo), CodigoDuplicado);
        }

        return ModelState.IsValid;
    }

    private static void VolcarDatos(ArticuloRequest request, Articulo articulo)
    {
        articulo.Codigo = request.Codigo.Trim();
        articulo.Descripcion = request.Descripcion.Trim();
        articulo.PrecioCosto = request.PrecioCosto;
        articulo.Margen = request.Margen;
        articulo.StockMinimo = request.StockMinimo;
        articulo.PuntoPedido = request.PuntoPedido;
        articulo.StockIdeal = request.StockIdeal;
    }

    private static ArticuloResponse ToResponse(Articulo a) => new(
        a.ArticuloId, a.Codigo, a.Descripcion, a.PrecioCosto, a.Margen,
        a.PrecioVenta, a.StockMinimo, a.PuntoPedido, a.StockIdeal);
}

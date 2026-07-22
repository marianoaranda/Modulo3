using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Stock.Web.Models;
using Stock.Web.Services;

namespace Stock.Web.Controllers;

/// <summary>Consulta de Stock Actual (RF-25), con exportación a Excel (AC-30).</summary>
public class StockController : Controller
{
    private const string HojaTitulo = "Stock Actual";

    private readonly IStockApiClient _api;

    public StockController(IStockApiClient api) => _api = api;

    /// <summary>Pantalla de consulta. Se ejecuta contra la API cuando el usuario aplica el rango.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(string? codigoInicial, string? codigoFinal, bool consultar = false)
    {
        // Al entrar sin rango cargado, sugerimos el primer y último código del catálogo.
        if (codigoInicial is null || codigoFinal is null)
        {
            var rango = await _api.ObtenerRangoCodigosAsync();
            codigoInicial ??= rango.Primero;
            codigoFinal ??= rango.Ultimo;
        }

        var model = new StockActualViewModel
        {
            CodigoInicial = codigoInicial,
            CodigoFinal = codigoFinal
        };

        if (consultar)
        {
            model.Filas = await _api.ConsultarStockActualAsync(codigoInicial, codigoFinal);
            model.Consultado = true;
        }

        return View(model);
    }

    /// <summary>Exporta a Excel la misma consulta que se ve en pantalla (AC-30).</summary>
    [HttpGet]
    public async Task<IActionResult> ExportarExcel(string? codigoInicial, string? codigoFinal)
    {
        var filas = await _api.ConsultarStockActualAsync(codigoInicial, codigoFinal);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(HojaTitulo);

        hoja.Cell(1, 1).Value = "Código";
        hoja.Cell(1, 2).Value = "Descripción";
        hoja.Cell(1, 3).Value = "Cantidad";
        hoja.Row(1).Style.Font.Bold = true;

        var fila = 2;
        foreach (var item in filas)
        {
            hoja.Cell(fila, 1).Value = item.Codigo;
            hoja.Cell(fila, 2).Value = item.Descripcion;
            hoja.Cell(fila, 3).Value = item.Cantidad;
            fila++;
        }

        hoja.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        libro.SaveAs(stream);

        const string tipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(stream.ToArray(), tipoXlsx, "StockActual.xlsx");
    }
}

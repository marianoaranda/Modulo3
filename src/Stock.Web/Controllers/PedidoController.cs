using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Stock.Web.Models;
using Stock.Web.Services;

namespace Stock.Web.Controllers;

/// <summary>Consulta Generar Pedido (RF-26), con exportación a Excel (AC-37).</summary>
public class PedidoController : Controller
{
    private const string HojaTitulo = "Generar Pedido";

    private readonly IStockApiClient _api;

    public PedidoController(IStockApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(bool soloBajoMinimo = false, ModoPedido modo = ModoPedido.HastaStockMinimo, bool consultar = false)
    {
        var model = new PedidoViewModel
        {
            SoloBajoMinimo = soloBajoMinimo,
            Modo = modo
        };

        if (consultar)
        {
            model.Filas = await _api.GenerarPedidoAsync(soloBajoMinimo, modo);
            model.Consultado = true;
        }

        return View(model);
    }

    /// <summary>Exporta a Excel la misma consulta que se ve en pantalla (AC-37).</summary>
    [HttpGet]
    public async Task<IActionResult> ExportarExcel(bool soloBajoMinimo = false, ModoPedido modo = ModoPedido.HastaStockMinimo)
    {
        var filas = await _api.GenerarPedidoAsync(soloBajoMinimo, modo);

        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(HojaTitulo);

        hoja.Cell(1, 1).Value = "Código";
        hoja.Cell(1, 2).Value = "Descripción";
        hoja.Cell(1, 3).Value = "Cantidad a Pedir";
        hoja.Row(1).Style.Font.Bold = true;

        var fila = 2;
        foreach (var item in filas)
        {
            hoja.Cell(fila, 1).Value = item.Codigo;
            hoja.Cell(fila, 2).Value = item.Descripcion;
            hoja.Cell(fila, 3).Value = item.CantidadAPedir;
            fila++;
        }

        hoja.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        libro.SaveAs(stream);

        const string tipoXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return File(stream.ToArray(), tipoXlsx, "GenerarPedido.xlsx");
    }
}

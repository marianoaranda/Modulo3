using Microsoft.AspNetCore.Mvc;
using Stock.Web.Models;
using Stock.Web.Services;

namespace Stock.Web.Controllers;

/// <summary>Pantallas de ABM de movimientos (RF-20 a RF-24). Consume la API vía el cliente, que adjunta el JWT.</summary>
public class MovimientosController : Controller
{
    private readonly IStockApiClient _api;

    public MovimientosController(IStockApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var movimientos = await _api.ListarMovimientosAsync();
        return View(movimientos);
    }

    [HttpGet]
    public IActionResult Create()
    {
        // Arranca con una línea vacía para que el formulario no aparezca sin detalle.
        var model = new MovimientoViewModel { Detalles = { new MovimientoDetalleViewModel() } };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MovimientoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _api.CrearMovimientoAsync(model.ToPayload());
        if (resultado.Exito)
        {
            TempData["Mensaje"] = $"Movimiento #{resultado.Movimiento!.Numero} creado.";
            return RedirectToAction(nameof(Index));
        }

        VolcarErrores(resultado.Errores);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var movimiento = await _api.ObtenerMovimientoAsync(id);
        return movimiento is null ? NotFound() : View(MovimientoViewModel.Desde(movimiento));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MovimientoViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _api.ModificarMovimientoAsync(id, model.ToPayload());
        if (resultado.Exito)
        {
            TempData["Mensaje"] = $"Movimiento #{resultado.Movimiento!.Numero} actualizado.";
            return RedirectToAction(nameof(Index));
        }

        VolcarErrores(resultado.Errores);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var movimiento = await _api.ObtenerMovimientoAsync(id);
        return movimiento is null ? NotFound() : View(movimiento);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _api.EliminarMovimientoAsync(id);
        TempData["Mensaje"] = "Movimiento eliminado.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Pasa los errores de la API al ModelState. Las claves de detalle ("Detalles") caen en el resumen.</summary>
    private void VolcarErrores(IReadOnlyDictionary<string, string[]> errores)
    {
        foreach (var (campo, mensajes) in errores)
        {
            // La API agrupa por "Detalles" o ""; los llevamos al resumen para no atarlos a un índice de fila puntual.
            var clave = campo == nameof(MovimientoViewModel.Detalles) ? string.Empty : campo;
            foreach (var mensaje in mensajes)
            {
                ModelState.AddModelError(clave, mensaje);
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Stock.Web.Models;
using Stock.Web.Services;

namespace Stock.Web.Controllers;

/// <summary>Pantallas de ABM de artículos (RF-13 a RF-19). Consume la API vía el cliente, que adjunta el JWT.</summary>
public class ArticulosController : Controller
{
    private readonly IStockApiClient _api;

    public ArticulosController(IStockApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(string? descripcion)
    {
        var articulos = await _api.ListarArticulosAsync(descripcion);
        ViewData["Descripcion"] = descripcion;
        return View(articulos);
    }

    [HttpGet]
    public IActionResult Create() => View(new ArticuloViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticuloViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _api.CrearArticuloAsync(model.ToPayload());
        if (resultado.Exito)
        {
            TempData["Mensaje"] = $"Artículo {resultado.Articulo!.Codigo} creado.";
            return RedirectToAction(nameof(Index));
        }

        VolcarErrores(resultado);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var articulo = await _api.ObtenerArticuloAsync(id);
        return articulo is null ? NotFound() : View(ArticuloViewModel.Desde(articulo));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ArticuloViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _api.ModificarArticuloAsync(id, model.ToPayload());
        if (resultado.Exito)
        {
            TempData["Mensaje"] = $"Artículo {resultado.Articulo!.Codigo} actualizado.";
            return RedirectToAction(nameof(Index));
        }

        VolcarErrores(resultado);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var articulo = await _api.ObtenerArticuloAsync(id);
        return articulo is null ? NotFound() : View(ArticuloViewModel.Desde(articulo));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _api.EliminarArticuloAsync(id);
        TempData["Mensaje"] = "Artículo eliminado.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Pasa los errores de la API al ModelState, respetando el campo (RF-17 va a Código; el resto al resumen).</summary>
    private void VolcarErrores(GuardarArticuloResultado resultado)
    {
        foreach (var (campo, mensajes) in resultado.Errores)
        {
            foreach (var mensaje in mensajes)
            {
                ModelState.AddModelError(campo, mensaje);
            }
        }
    }
}

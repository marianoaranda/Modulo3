using System.ComponentModel.DataAnnotations;
using Stock.Web.Services;

namespace Stock.Web.Models;

/// <summary>Consulta de Stock Actual (RF-25): parámetros de rango y filas resultantes.</summary>
public class StockActualViewModel
{
    [Display(Name = "Artículo inicial")]
    public string? CodigoInicial { get; set; }

    [Display(Name = "Artículo final")]
    public string? CodigoFinal { get; set; }

    /// <summary>True una vez que se ejecutó la consulta, para distinguir "sin datos" de "todavía no consultó".</summary>
    public bool Consultado { get; set; }

    public IReadOnlyList<StockActualDto> Filas { get; set; } = new List<StockActualDto>();
}

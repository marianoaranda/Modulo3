using System.ComponentModel.DataAnnotations;
using Stock.Web.Services;

namespace Stock.Web.Models;

/// <summary>Formulario de alta/edición de artículos. Las reglas de negocio finas las valida la API (RF-17 a RF-19).</summary>
public class ArticuloViewModel
{
    public int ArticuloId { get; set; }

    [Required(ErrorMessage = "Ingresá el código.")]
    [StringLength(50)]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresá la descripción.")]
    [StringLength(200)]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo.")]
    [Display(Name = "Precio de Costo")]
    public decimal PrecioCosto { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo.")]
    [Display(Name = "Margen (%)")]
    public decimal Margen { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "No puede ser negativo.")]
    [Display(Name = "Stock Mínimo")]
    public int StockMinimo { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "No puede ser negativo.")]
    [Display(Name = "Punto de Pedido")]
    public int PuntoPedido { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "No puede ser negativo.")]
    [Display(Name = "Stock Ideal")]
    public int StockIdeal { get; set; }

    /// <summary>Precio de Venta calculado por la API (RF-16); sólo lectura en la pantalla.</summary>
    [Display(Name = "Precio de Venta")]
    public decimal PrecioVenta { get; set; }

    public bool EsNuevo => ArticuloId == 0;

    public ArticuloPayload ToPayload() =>
        new(Codigo, Descripcion, PrecioCosto, Margen, StockMinimo, PuntoPedido, StockIdeal);

    public static ArticuloViewModel Desde(ArticuloDto dto) => new()
    {
        ArticuloId = dto.ArticuloId,
        Codigo = dto.Codigo,
        Descripcion = dto.Descripcion,
        PrecioCosto = dto.PrecioCosto,
        Margen = dto.Margen,
        StockMinimo = dto.StockMinimo,
        PuntoPedido = dto.PuntoPedido,
        StockIdeal = dto.StockIdeal,
        PrecioVenta = dto.PrecioVenta
    };
}

using System.ComponentModel.DataAnnotations;
using Stock.Web.Services;

namespace Stock.Web.Models;

/// <summary>Formulario de alta/edición de un movimiento con su detalle (RF-20 a RF-24).</summary>
public class MovimientoViewModel
{
    public int MovimientoId { get; set; }

    [Display(Name = "Tipo")]
    public TipoMovimiento Tipo { get; set; } = TipoMovimiento.Compra;

    [Display(Name = "Número")]
    [Range(1, int.MaxValue, ErrorMessage = "Ingresá un número de comprobante.")]
    public int Numero { get; set; }

    [Display(Name = "Fecha")]
    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today;

    public List<MovimientoDetalleViewModel> Detalles { get; set; } = new();

    public bool EsNuevo => MovimientoId == 0;

    public MovimientoPayload ToPayload() =>
        new(Tipo.ToString(), Numero, Fecha, Detalles.Select(d => d.ToPayload()).ToList());

    public static MovimientoViewModel Desde(MovimientoDto dto) => new()
    {
        MovimientoId = dto.MovimientoId,
        Tipo = Enum.TryParse<TipoMovimiento>(dto.Tipo, out var tipo) ? tipo : TipoMovimiento.Compra,
        Numero = dto.Numero,
        Fecha = dto.Fecha,
        Detalles = dto.Detalles.Select(d => new MovimientoDetalleViewModel
        {
            Codigo = d.Codigo,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario
        }).ToList()
    };
}

public class MovimientoDetalleViewModel
{
    [Required(ErrorMessage = "Ingresá el código.")]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que 0.")]
    [Display(Name = "Cantidad")]
    public int Cantidad { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "No puede ser negativo.")]
    [Display(Name = "Precio Unitario")]
    public decimal PrecioUnitario { get; set; }

    public MovimientoDetallePayload ToPayload() => new(Codigo.Trim(), Cantidad, PrecioUnitario);
}

using System.ComponentModel.DataAnnotations;
using Stock.Web.Services;

namespace Stock.Web.Models;

/// <summary>Consulta Generar Pedido (RF-26): parámetros y filas con la cantidad a pedir.</summary>
public class PedidoViewModel
{
    [Display(Name = "Solo bajo mínimo")]
    public bool SoloBajoMinimo { get; set; }

    [Display(Name = "Modo de pedido")]
    public ModoPedido Modo { get; set; } = ModoPedido.HastaStockMinimo;

    /// <summary>True una vez ejecutada la consulta, para distinguir "sin datos" de "todavía no consultó".</summary>
    public bool Consultado { get; set; }

    public IReadOnlyList<PedidoDto> Filas { get; set; } = new List<PedidoDto>();
}

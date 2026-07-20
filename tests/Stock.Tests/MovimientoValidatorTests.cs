using Stock.Api.Domain;

namespace Stock.Tests;

[TestFixture]
public class MovimientoValidatorTests
{
    private static Movimiento ConDetalles(params int[] cantidades)
    {
        var mov = new Movimiento { Tipo = TipoMovimiento.Venta, Numero = 1, Fecha = DateTime.UtcNow };
        foreach (var cantidad in cantidades)
        {
            mov.Detalles.Add(new MovimientoDetalle { ArticuloId = 1, Cantidad = cantidad, PrecioUnitario = 10, PrecioTotal = 10 * cantidad });
        }
        return mov;
    }

    [Test]
    public void Validar_MovimientoConLineasValidas_NoDevuelveErrores()
    {
        Assert.That(MovimientoValidator.Validar(ConDetalles(1, 5, 10)), Is.Empty);
    }

    [Test]
    public void Validar_SinLineas_RechazaConElMensajeDeDetalle()
    {
        Assert.That(MovimientoValidator.Validar(ConDetalles()), Does.Contain(MovimientoValidator.SinDetalle));
    }

    [TestCase(0)]
    [TestCase(-3)]
    public void Validar_ConCantidadNoPositiva_Rechaza(int cantidad)
    {
        // AC-26 (RF-23): cantidad 0 o negativa invalida el movimiento.
        Assert.That(MovimientoValidator.Validar(ConDetalles(2, cantidad)),
            Does.Contain(MovimientoValidator.CantidadInvalida));
    }

    [Test]
    public void ArticulosConStockInsuficiente_CuandoLaVentaNoSuperaElSaldo_NoDevuelveNada()
    {
        // AC-28 (RF-24): vender 5 con saldo 5 deja el stock en 0, que es válido.
        var vendida = new Dictionary<int, int> { [1] = 5 };
        var saldo = new Dictionary<int, int> { [1] = 5 };

        Assert.That(MovimientoValidator.ArticulosConStockInsuficiente(vendida, saldo), Is.Empty);
    }

    [Test]
    public void ArticulosConStockInsuficiente_CuandoLaVentaSuperaElSaldo_DevuelveElArticulo()
    {
        // AC-27 (RF-24): vender 6 con saldo 5 dejaría el stock en -1.
        var vendida = new Dictionary<int, int> { [1] = 6 };
        var saldo = new Dictionary<int, int> { [1] = 5 };

        Assert.That(MovimientoValidator.ArticulosConStockInsuficiente(vendida, saldo), Is.EqualTo(new[] { 1 }));
    }

    [Test]
    public void ArticulosConStockInsuficiente_ArticuloSinMovimientos_SeTomaComoSaldoCero()
    {
        // Un artículo que no está en el diccionario de saldos vale 0: cualquier venta lo deja negativo.
        var vendida = new Dictionary<int, int> { [99] = 1 };
        var saldo = new Dictionary<int, int>();

        Assert.That(MovimientoValidator.ArticulosConStockInsuficiente(vendida, saldo), Is.EqualTo(new[] { 99 }));
    }
}

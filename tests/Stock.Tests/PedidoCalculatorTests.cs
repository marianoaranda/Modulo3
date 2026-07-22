using Stock.Api.Domain;

namespace Stock.Tests;

/// <summary>
/// Cálculo de la Cantidad a Pedir (RF-26 / AC-31 a AC-36). Un artículo de referencia:
/// StockMínimo 10, PuntoPedido 20, StockIdeal 30 (respeta mínimo ≤ punto ≤ ideal, RF-19).
/// </summary>
[TestFixture]
public class PedidoCalculatorTests
{
    private const int StockMinimo = 10;
    private const int PuntoPedido = 20;
    private const int StockIdeal = 30;

    private static int? Calcular(int saldo, ModoPedido modo, bool soloBajoMinimo)
    {
        var objetivo = PedidoCalculator.NivelObjetivo(StockMinimo, PuntoPedido, StockIdeal, modo);
        return PedidoCalculator.CantidadAPedir(saldo, StockMinimo, objetivo, soloBajoMinimo);
    }

    [Test]
    public void AC31_NoSoloBajoMinimo_HastaStockMinimo_PideHastaElMinimo()
    {
        // saldo 4 => MAX(0, 10 − 4) = 6
        Assert.That(Calcular(4, ModoPedido.HastaStockMinimo, soloBajoMinimo: false), Is.EqualTo(6));
    }

    [Test]
    public void AC32_NoSoloBajoMinimo_HastaPuntoPedido_PideHastaElPunto()
    {
        // saldo 4 => MAX(0, 20 − 4) = 16
        Assert.That(Calcular(4, ModoPedido.HastaPuntoPedido, soloBajoMinimo: false), Is.EqualTo(16));
    }

    [Test]
    public void AC33_NoSoloBajoMinimo_HastaStockIdeal_PideHastaElIdeal()
    {
        // saldo 4 => MAX(0, 30 − 4) = 26
        Assert.That(Calcular(4, ModoPedido.HastaStockIdeal, soloBajoMinimo: false), Is.EqualTo(26));
    }

    [Test]
    public void AC34_SoloBajoMinimo_HastaStockMinimo_SoloLosQueEstanBajoMinimo()
    {
        // saldo 4 (< mínimo) => 10 − 4 = 6
        Assert.That(Calcular(4, ModoPedido.HastaStockMinimo, soloBajoMinimo: true), Is.EqualTo(6));
    }

    [Test]
    public void AC35_SoloBajoMinimo_HastaPuntoPedido_SoloLosQueEstanBajoMinimo()
    {
        // saldo 4 (< mínimo) => 20 − 4 = 16
        Assert.That(Calcular(4, ModoPedido.HastaPuntoPedido, soloBajoMinimo: true), Is.EqualTo(16));
    }

    [Test]
    public void AC36_SoloBajoMinimo_HastaStockIdeal_SoloLosQueEstanBajoMinimo()
    {
        // saldo 4 (< mínimo) => 30 − 4 = 26
        Assert.That(Calcular(4, ModoPedido.HastaStockIdeal, soloBajoMinimo: true), Is.EqualTo(26));
    }

    [Test]
    public void SoloBajoMinimo_ConSaldoEnElMinimo_ExcluyeAlArticulo()
    {
        // saldo 10 (= mínimo, no está por debajo) => no entra en el pedido.
        Assert.That(Calcular(10, ModoPedido.HastaStockIdeal, soloBajoMinimo: true), Is.Null);
    }

    [Test]
    public void NoSoloBajoMinimo_ConSaldoSobreElObjetivo_PideCero()
    {
        // saldo 25 y objetivo mínimo 10 => MAX(0, 10 − 25) = 0 (el artículo aparece con 0).
        Assert.That(Calcular(25, ModoPedido.HastaStockMinimo, soloBajoMinimo: false), Is.EqualTo(0));
    }
}

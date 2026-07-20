using Stock.Api.Domain;

namespace Stock.Tests;

[TestFixture]
public class ArticuloValidatorTests
{
    /// <summary>Artículo base válido; cada test corre una sola regla partiendo de acá.</summary>
    private static Articulo ArticuloValido() => new()
    {
        Codigo = "A001",
        Descripcion = "Artículo de prueba",
        PrecioCosto = 100m,
        Margen = 50m,
        StockMinimo = 5,
        PuntoPedido = 10,
        StockIdeal = 20
    };

    [Test]
    public void CalcularPrecioVenta_AplicaCostoPorUnoMasMargen()
    {
        // AC-19 (RF-16): 100 × (1 + 50/100) = 150
        Assert.That(Articulo.CalcularPrecioVenta(100m, 50m), Is.EqualTo(150m));
    }

    [Test]
    public void PrecioVenta_DelArticulo_UsaLaFormula()
    {
        // AC-19 (RF-16), a través de la propiedad derivada
        var articulo = ArticuloValido();

        Assert.That(articulo.PrecioVenta, Is.EqualTo(150m));
    }

    [Test]
    public void Validar_ArticuloCorrecto_NoDevuelveErrores()
    {
        Assert.That(ArticuloValidator.Validar(ArticuloValido()), Is.Empty);
    }

    [TestCase(nameof(Articulo.PrecioCosto))]
    [TestCase(nameof(Articulo.Margen))]
    [TestCase(nameof(Articulo.StockMinimo))]
    [TestCase(nameof(Articulo.PuntoPedido))]
    [TestCase(nameof(Articulo.StockIdeal))]
    public void Validar_ConUnCampoNegativo_RechazaConElMensajeDeValoresNegativos(string campo)
    {
        // AC-21 (RF-18): cualquiera de estos campos en negativo invalida el artículo.
        var articulo = ArticuloValido();
        switch (campo)
        {
            case nameof(Articulo.PrecioCosto): articulo.PrecioCosto = -1m; break;
            case nameof(Articulo.Margen): articulo.Margen = -1m; break;
            // Se rompen juntos para no violar además el orden Mínimo ≤ Punto ≤ Ideal.
            case nameof(Articulo.StockMinimo): articulo.StockMinimo = -5; break;
            case nameof(Articulo.PuntoPedido): articulo.PuntoPedido = -1; articulo.StockMinimo = -5; break;
            case nameof(Articulo.StockIdeal): articulo.StockIdeal = -1; articulo.PuntoPedido = -5; articulo.StockMinimo = -10; break;
        }

        Assert.That(ArticuloValidator.Validar(articulo), Does.Contain(ArticuloValidator.ValoresNegativos));
    }

    [TestCase(15, 10, 20, TestName = "Minimo mayor que Punto")]
    [TestCase(5, 25, 20, TestName = "Punto mayor que Ideal")]
    [TestCase(20, 15, 10, TestName = "Todo al reves")]
    public void Validar_ConNivelesDesordenados_RechazaConElMensajeDeOrden(int minimo, int punto, int ideal)
    {
        // AC-22 (RF-19): debe cumplirse Mínimo ≤ Punto ≤ Ideal.
        var articulo = ArticuloValido();
        articulo.StockMinimo = minimo;
        articulo.PuntoPedido = punto;
        articulo.StockIdeal = ideal;

        Assert.That(ArticuloValidator.Validar(articulo), Does.Contain(ArticuloValidator.OrdenDeStock));
    }

    [Test]
    public void Validar_ConNivelesIguales_EsValido()
    {
        // RF-19 usa ≤: Mínimo = Punto = Ideal es un caso límite aceptable.
        var articulo = ArticuloValido();
        articulo.StockMinimo = articulo.PuntoPedido = articulo.StockIdeal = 7;

        Assert.That(ArticuloValidator.Validar(articulo), Is.Empty);
    }
}

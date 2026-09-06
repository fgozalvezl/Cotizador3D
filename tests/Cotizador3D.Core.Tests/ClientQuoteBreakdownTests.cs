using Cotizador3D.Core.Calculation;
using Cotizador3D.Core.Models;
using Xunit;

namespace Cotizador3D.Core.Tests;

public class ClientQuoteBreakdownTests
{
    [Fact]
    public void ParaCliente_CadaLineaEsElCostoPorElMargen()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());
        var desglose = resultado.ParaCliente();

        Assert.Equal(3750d, desglose.Material, 8);          // 2500 * 1,5
        Assert.Equal(112.35735d, desglose.Luz, 8);          // 74,9049 * 1,5
        Assert.Equal(23.5950435d, desglose.IvaLuz, 8);      // 15,730029 * 1,5
        Assert.Equal(228.75d, desglose.Desgaste, 8);        // 152,5 * 1,5
        Assert.Equal(409.110735d, desglose.MargenError, 8); // 272,74049 * 1,5
        Assert.Equal(0d, desglose.Envio);
    }

    [Fact]
    public void ParaCliente_LasLineasSumanExactamenteElTotal()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());
        var desglose = resultado.ParaCliente();

        Assert.Equal(resultado.PrecioFinalConEnvio, desglose.Total);
        Assert.Equal(desglose.Total, desglose.SumaDeLineas);
    }

    [Fact]
    public void ParaCliente_ConEnvio_LasLineasSumanExactamenteElTotal()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Settings.CostoEnvio = 3500;

        var resultado = QuoteCalculator.Calcular(entrada);
        var desglose = resultado.ParaCliente();

        Assert.Equal(3500d, desglose.Envio);
        Assert.Equal(resultado.PrecioFinalConEnvio, desglose.Total);
        Assert.Equal(desglose.Total, desglose.SumaDeLineas);
        Assert.Contains(desglose.Lineas, l => l.Concepto == ClientQuoteBreakdown.ConceptoEnvio);
    }

    [Theory]
    [InlineData(1d)]
    [InlineData(1.5d)]
    [InlineData(2d)]
    [InlineData(3.33d)]
    public void ParaCliente_SumaExacta_ConDistintosMargenes(double margen)
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Settings.MargenGanancia = margen;
        entrada.Settings.CostoEnvio = 1234.56;

        var desglose = QuoteCalculator.Calcular(entrada).ParaCliente();

        Assert.Equal(desglose.Total, desglose.SumaDeLineas);
    }

    [Fact]
    public void ParaCliente_SinEnvio_NoMuestraLaLineaDeEnvio()
    {
        var desglose = QuoteCalculator.Calcular(TestData.EntradaPorDefecto()).ParaCliente();

        Assert.DoesNotContain(desglose.Lineas, l => l.Concepto == ClientQuoteBreakdown.ConceptoEnvio);
        Assert.Equal(5, desglose.Lineas.Count);
    }

    [Fact]
    public void ParaCliente_NoExponeLaGanancia()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());
        var desglose = resultado.ParaCliente();

        foreach (var linea in desglose.Lineas)
        {
            Assert.DoesNotContain("ganancia", linea.Concepto, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("venta", linea.Concepto, StringComparison.OrdinalIgnoreCase);

            // Ninguna linea revela el costo real ni el multiplicador.
            Assert.NotEqual(resultado.CostoTotal, linea.Importe, 6);
            Assert.NotEqual(resultado.MargenGanancia, linea.Importe, 6);
        }

        // El costo total sin ganancia no aparece por ningun lado del desglose.
        Assert.NotEqual(resultado.CostoTotal, desglose.SumaDeLineas, 6);
    }

    [Fact]
    public void ParaCliente_LasLineasEstanEnOrdenYRotuladasEnEspanol()
    {
        var desglose = QuoteCalculator.Calcular(TestData.EntradaPorDefecto()).ParaCliente();

        Assert.Collection(
            desglose.Lineas,
            l => Assert.Equal(ClientQuoteBreakdown.ConceptoMaterial, l.Concepto),
            l => Assert.Equal(ClientQuoteBreakdown.ConceptoLuz, l.Concepto),
            l => Assert.Equal("IVA energía (21%)", l.Concepto),
            l => Assert.Equal(ClientQuoteBreakdown.ConceptoDesgaste, l.Concepto),
            l => Assert.Equal(ClientQuoteBreakdown.ConceptoMargenError, l.Concepto));
    }

    [Fact]
    public void ParaCliente_ElRotuloDeIvaUsaElPorcentajeConfigurado()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Settings.IvaLuzPct = 10.5;

        var desglose = QuoteCalculator.Calcular(entrada).ParaCliente();

        Assert.Equal("IVA energía (10,5%)", desglose.ConceptoIvaLuz);
    }

    [Fact]
    public void Crear_ResultadoNulo_Lanza()
    {
        Assert.Throws<ArgumentNullException>(() => ClientQuoteBreakdown.Crear(null!));
    }
}

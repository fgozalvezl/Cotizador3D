using Cotizador3D.Core.Calculation;
using Cotizador3D.Core.Formatting;
using Cotizador3D.Core.Models;
using Xunit;

namespace Cotizador3D.Core.Tests;

public class ClientQuoteBreakdownTests
{
    [Fact]
    public void ParaCliente_CadaLineaEsElCostoPorElMargenRedondeadoADosDecimales()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());
        var desglose = resultado.ParaCliente();

        Assert.Equal(3750d, desglose.Material, 8);     // 2500 * 1,5      = 3750
        Assert.Equal(112.36d, desglose.Luz, 8);        // 74,9049 * 1,5   = 112,35735
        Assert.Equal(23.60d, desglose.IvaLuz, 8);      // 15,730029 * 1,5 = 23,5950435
        Assert.Equal(228.75d, desglose.Desgaste, 8);   // 152,5 * 1,5     = 228,75
        Assert.Equal(0d, desglose.Envio);

        // El margen de error cierra la suma: 4523,81 (precio de venta
        // redondeado) - 4114,71 (las otras cuatro lineas redondeadas).
        Assert.Equal(409.10d, desglose.MargenError, 8);
        Assert.Equal(4523.8131285d, resultado.PrecioVenta, 6);
        Assert.Equal(4523.81d, desglose.Total, 8);
    }

    /// <summary>
    /// La invariante que importa: los importes REDONDEADOS (los que se
    /// imprimen) suman exactamente el total impreso. Antes cada linea se
    /// redondeaba por separado al imprimir y la columna no cerraba.
    /// </summary>
    [Fact]
    public void ParaCliente_LasLineasRedondeadasSumanElTotal_EnTodaLaMatriz()
    {
        var casos = 0;

        foreach (var precioKg in new[] { 18500d, 25000.5d })
        {
            foreach (var margen in new[] { 1d, 1.5d, 2.4d, 3d })
            {
                foreach (var envio in new[] { 0d, 3500d })
                {
                    for (var gramos = 1d; gramos <= 500d; gramos += 7d)
                    {
                        var entrada = TestData.EntradaPorDefecto();
                        entrada.Gramos = gramos;
                        entrada.Filamento!.PriceKg = precioKg;
                        entrada.Settings.MargenGanancia = margen;
                        entrada.Settings.CostoEnvio = envio;

                        var resultado = QuoteCalculator.Calcular(entrada);
                        var desglose = resultado.ParaCliente();
                        var contexto = $"gramos={gramos}, margen={margen}, envio={envio}, precio_kg={precioKg}";

                        // 1) Cada linea ya viene redondeada a 2 decimales.
                        foreach (var linea in desglose.Lineas)
                        {
                            Assert.Equal(
                                Math.Round(linea.Importe, 2, MidpointRounding.AwayFromZero),
                                linea.Importe);

                            // Y ningun importe se imprime como "-0,00".
                            Assert.DoesNotContain("-0,00", MoneyFormat.Moneda(linea.Importe), StringComparison.Ordinal);
                        }

                        // 2) La suma de las lineas impresas es el total impreso.
                        var sumaImpresa = desglose.Lineas
                            .Aggregate(0m, (acc, l) => acc + ImportesDelTexto.ComoSeImprime(l.Importe));

                        Assert.Equal(ImportesDelTexto.ComoSeImprime(desglose.Total), sumaImpresa);
                        Assert.Equal(desglose.Total, desglose.SumaDeLineas);

                        // 3) El total es el precio de venta redondeado mas el envio redondeado.
                        Assert.Equal(
                            Math.Round(resultado.PrecioVenta, 2, MidpointRounding.AwayFromZero)
                            + Math.Round(resultado.CostoEnvio, 2, MidpointRounding.AwayFromZero),
                            desglose.Total,
                            8);

                        // 4) Y sigue siendo el precio final, salvo el centavo del redondeo.
                        Assert.True(
                            Math.Abs(desglose.Total - resultado.PrecioFinalConEnvio) <= 0.005d,
                            contexto);

                        casos++;
                    }
                }
            }
        }

        Assert.Equal(1152, casos);
    }

    /// <summary>Un residuo negativo minusculo no puede imprimirse como "-0,00".</summary>
    [Fact]
    public void ParaCliente_ImportesDespreciables_SeMuestranComoCero()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Settings.MargenGanancia = 0.0000001;
        entrada.Settings.CostoEnvio = 0;

        var desglose = QuoteCalculator.Calcular(entrada).ParaCliente();

        foreach (var linea in desglose.Lineas)
        {
            Assert.False(double.IsNegative(linea.Importe) && linea.Importe == 0d);
            Assert.DoesNotContain("-0,00", MoneyFormat.Moneda(linea.Importe), StringComparison.Ordinal);
        }

        Assert.Equal(desglose.Total, desglose.SumaDeLineas);
    }

    [Fact]
    public void ParaCliente_LasLineasSumanExactamenteElTotal()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());
        var desglose = resultado.ParaCliente();

        Assert.Equal(
            Math.Round(resultado.PrecioFinalConEnvio, 2, MidpointRounding.AwayFromZero),
            desglose.Total,
            8);
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
        Assert.Equal(
            Math.Round(resultado.PrecioFinalConEnvio, 2, MidpointRounding.AwayFromZero),
            desglose.Total,
            8);
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

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

        Assert.Equal(112.36d, desglose.Luz, 8);        // 74,9049 * 1,5    = 112,35735
        Assert.Equal(23.60d, desglose.IvaLuz, 8);      // 15,730029 * 1,5  = 23,5950435
        Assert.Equal(228.75d, desglose.Desgaste, 8);   // 152,5 * 1,5      = 228,75
        Assert.Equal(409.11d, desglose.MargenError, 8);// 272,74049 * 1,5  = 409,110735
        Assert.Equal(0d, desglose.Envio);

        // El material cierra la suma: 4523,81 (total) - 773,82 (las otras
        // cuatro lineas redondeadas). Su valor "puro" seria 2500 * 1,5 = 3750.
        Assert.Equal(3749.99d, desglose.Material, 8);
        Assert.Equal(4523.8131285d, resultado.PrecioVenta, 6);
        Assert.Equal(4523.81d, desglose.Total, 8);
        Assert.Equal(desglose.Total, desglose.SumaDeLineas);
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

        // precio_kg = 0 esta permitido por el validador: el material queda en 0
        // y la linea mas grande pasa a ser otra.
        foreach (var precioKg in new[] { 18500d, 25000.5d, 0d })
        {
            foreach (var margen in new[] { 1d, 1.5d, 2.4d, 3d })
            {
                foreach (var envio in new[] { 0d, 3500d })
                {
                    foreach (var margenErrorPct in new[] { 10d, 0d })
                    {
                        for (var gramos = 1d; gramos <= 500d; gramos += 7d)
                        {
                            var entrada = TestData.EntradaPorDefecto();
                            entrada.Gramos = gramos;
                            entrada.Filamento!.PriceKg = precioKg;
                            entrada.Settings.MargenGanancia = margen;
                            entrada.Settings.CostoEnvio = envio;
                            entrada.Settings.MargenErrorPct = margenErrorPct;

                            var resultado = QuoteCalculator.Calcular(entrada);
                            var desglose = resultado.ParaCliente();
                            var contexto = $"gramos={gramos}, margen={margen}, envio={envio}, " +
                                $"precio_kg={precioKg}, margen_error={margenErrorPct}";

                            // 1) Cada linea ya viene redondeada a 2 decimales.
                            foreach (var linea in desglose.Lineas)
                            {
                                Assert.Equal(
                                    Math.Round(linea.Importe, 2, MidpointRounding.AwayFromZero),
                                    linea.Importe);

                                // Ninguna linea es negativa: el residuo lo absorbe la mas grande.
                                Assert.True(linea.Importe >= 0d, $"{linea.Concepto} < 0 con {contexto}");

                                // Y ningun importe se imprime como "-0,00".
                                Assert.DoesNotContain("-0,00", MoneyFormat.Moneda(linea.Importe), StringComparison.Ordinal);
                            }

                            // 2) La suma de las lineas impresas es el total impreso.
                            var sumaImpresa = desglose.Lineas
                                .Aggregate(0m, (acc, l) => acc + ImportesDelTexto.ComoSeImprime(l.Importe));

                            Assert.Equal(ImportesDelTexto.ComoSeImprime(desglose.Total), sumaImpresa);
                            Assert.Equal(desglose.Total, desglose.SumaDeLineas);

                            // 3) El total es el precio final con envio, redondeado.
                            Assert.Equal(
                                Math.Round(resultado.PrecioFinalConEnvio, 2, MidpointRounding.AwayFromZero),
                                desglose.Total,
                                8);

                            // 4) La linea de margen de error solo aparece si el porcentaje es > 0.
                            Assert.Equal(
                                margenErrorPct > 0,
                                desglose.Lineas.Any(l => l.Concepto == ClientQuoteBreakdown.ConceptoMargenError));

                            casos++;
                        }
                    }
                }
            }
        }

        Assert.Equal(3456, casos);
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

    /// <summary>
    /// El caso del reviewer: con margen de error 0% la linea desaparece (antes
    /// se imprimia "$ -0,01", el residuo del redondeo) y la columna sigue
    /// cerrando exactamente contra el total.
    /// </summary>
    [Fact]
    public void ParaCliente_SinMargenDeError_NoMuestraLaLineaYLaSumaCierra()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Gramos = 250;
        entrada.Filamento!.PriceKg = 24990;
        entrada.Horas = 0;
        entrada.Minutos = 30;
        entrada.Settings.ConsumoW = 220;
        entrada.Settings.PrecioKwh = 75;
        entrada.Settings.PrecioRepuestos = 120000;
        entrada.Settings.DesgasteHoras = 1000;
        entrada.Settings.MargenGanancia = 1.5;
        entrada.Settings.MargenErrorPct = 0;
        entrada.Settings.CostoEnvio = 0;

        var resultado = QuoteCalculator.Calcular(entrada);
        var desglose = resultado.ParaCliente();

        Assert.DoesNotContain(desglose.Lineas, l => l.Concepto == ClientQuoteBreakdown.ConceptoMargenError);
        Assert.Equal(4, desglose.Lineas.Count);

        // La propiedad sigue poblada, con 0.
        Assert.Equal(0d, desglose.MargenError);

        // Ninguna linea negativa: el residuo lo absorbe la linea mas grande,
        // que en este caso es la de material.
        Assert.All(desglose.Lineas, l => Assert.True(l.Importe >= 0d, l.Concepto));
        Assert.Equal(9371.24d, desglose.Material, 8);   // 9371,25 - 0,01

        Assert.Equal(9476.22d, desglose.Total, 8);
        Assert.Equal(desglose.Total, desglose.SumaDeLineas);
        Assert.Equal(
            Math.Round(resultado.PrecioFinalConEnvio, 2, MidpointRounding.AwayFromZero),
            desglose.Total,
            8);
    }

    /// <summary>
    /// El total se ancla en <c>round(precio_final_con_envio)</c> (lo que muestra
    /// la ventana principal), no en <c>round(precio_venta) + round(envio)</c>,
    /// que puede diferir en un centavo.
    /// </summary>
    [Fact]
    public void ParaCliente_ElTotalEsElPrecioFinalRedondeado_AunqueLasPartesRedondeenDistinto()
    {
        var resultado = new QuoteResult
        {
            HorasImpresion = 1,
            PrecioMaterial = 88.106,
            PrecioLuz = 10,
            DesgasteMaquina = 0,
            CostoBase = 98.106,
            MargenErrorValor = 0,
            IvaLuzValor = 2.1,
            CostoTotal = 100.206,
            PrecioVenta = 100.206,
            PrecioFinalConEnvio = 110.212,
            CostoEnvio = 10.006,
            MargenGanancia = 1,
            IvaLuzPct = 21,
            MargenErrorPct = 0,
        };

        // Las partes redondeadas por separado dan un centavo de mas.
        Assert.NotEqual(
            Math.Round(resultado.PrecioVenta, 2, MidpointRounding.AwayFromZero)
            + Math.Round(resultado.CostoEnvio, 2, MidpointRounding.AwayFromZero),
            Math.Round(resultado.PrecioFinalConEnvio, 2, MidpointRounding.AwayFromZero));

        var desglose = resultado.ParaCliente();

        Assert.Equal(
            Math.Round(resultado.PrecioFinalConEnvio, 2, MidpointRounding.AwayFromZero),
            desglose.Total,
            8);
        Assert.Equal(110.21d, desglose.Total, 8);
        Assert.Equal(10.01d, desglose.Envio, 8);
        Assert.Equal(desglose.Total, desglose.SumaDeLineas);
        Assert.All(desglose.Lineas, l => Assert.True(l.Importe >= 0d, l.Concepto));
    }

    /// <summary>
    /// Material en 0 (precio por kilo 0, que el validador acepta) y residuo
    /// negativo: el residuo va a la linea mas grande (desgaste), no al
    /// material, que si no quedaria en "$ -0,01".
    /// </summary>
    [Fact]
    public void ParaCliente_MaterialEnCero_ElResiduoVaALaLineaMasGrande()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Gramos = 1;
        entrada.Filamento!.PriceKg = 0;
        entrada.Settings.ConsumoW = 5;
        entrada.Settings.PrecioKwh = 0.5;
        entrada.Settings.PrecioRepuestos = 10;
        entrada.Settings.DesgasteHoras = 1000;
        entrada.Settings.MargenGanancia = 1;
        entrada.Settings.MargenErrorPct = 0;
        entrada.Settings.CostoEnvio = 0;

        var resultado = QuoteCalculator.Calcular(entrada);
        var desglose = resultado.ParaCliente();

        // Lineas "puras": 0 + 0,01 + 0,00 + 0,03 = 0,04 contra un total de 0,03.
        Assert.Equal(-0.01d, Math.Round(
            Math.Round(resultado.PrecioFinalConEnvio, 2, MidpointRounding.AwayFromZero)
            - (Math.Round(resultado.PrecioLuz, 2, MidpointRounding.AwayFromZero)
               + Math.Round(resultado.DesgasteMaquina, 2, MidpointRounding.AwayFromZero)), 2));

        Assert.Equal(0d, desglose.Material);
        Assert.Equal(0.01d, desglose.Luz, 8);
        Assert.Equal(0d, desglose.IvaLuz);
        Assert.Equal(0.02d, desglose.Desgaste, 8);   // 0,03 - 0,01 de residuo
        Assert.Equal(0.03d, desglose.Total, 8);

        Assert.All(desglose.Lineas, l => Assert.True(l.Importe >= 0d, l.Concepto));
        Assert.All(desglose.Lineas, l =>
            Assert.DoesNotContain("-0,0", MoneyFormat.Moneda(l.Importe), StringComparison.Ordinal));
        Assert.Equal(desglose.Total, desglose.SumaDeLineas);
        Assert.Equal(
            Math.Round(resultado.PrecioFinalConEnvio, 2, MidpointRounding.AwayFromZero),
            desglose.Total,
            8);
    }

    [Fact]
    public void ParaCliente_SinEnvio_NoMuestraLaLineaDeEnvio()
    {
        var desglose = QuoteCalculator.Calcular(TestData.EntradaPorDefecto()).ParaCliente();

        Assert.DoesNotContain(desglose.Lineas, l => l.Concepto == ClientQuoteBreakdown.ConceptoEnvio);
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

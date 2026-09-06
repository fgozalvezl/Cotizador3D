using Cotizador3D.Core.Calculation;
using Cotizador3D.Core.Models;
using Xunit;

namespace Cotizador3D.Core.Tests;

public class QuoteCalculatorTests
{
    // Valores calculados a mano con los defaults del legado:
    //   material = (100 / 1000) * 25000            = 2500
    //   luz      = (150 / 1000) * 2,5 * 199,7464   = 74,9049
    //   desgaste = (305000 / 5000) * 2,5           = 152,5
    //   base     = 2500 + 74,9049 + 152,5          = 2727,4049
    //   error    = 2727,4049 * 0,10                = 272,74049
    //   iva luz  = 74,9049 * 0,21                  = 15,730029
    //   total    = 2727,4049 + 272,74049 + 15,7300 = 3015,875419
    //   venta    = 3015,875419 * 1,5               = 4523,8131285
    private const double MaterialEsperado = 2500d;
    private const double LuzEsperada = 74.9049d;
    private const double DesgasteEsperado = 152.5d;
    private const double BaseEsperada = 2727.4049d;
    private const double MargenErrorEsperado = 272.74049d;
    private const double IvaLuzEsperado = 15.730029d;
    private const double CostoTotalEsperado = 3015.875419d;
    private const double PrecioVentaEsperado = 4523.8131285d;

    [Fact]
    public void Calcular_EscenarioDeReferencia_ReproduceLasFormulasDelLegado()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());

        Assert.Equal(2.5d, resultado.HorasImpresion, 10);
        Assert.Equal(MaterialEsperado, resultado.PrecioMaterial, 10);
        Assert.Equal(LuzEsperada, resultado.PrecioLuz, 10);
        Assert.Equal(DesgasteEsperado, resultado.DesgasteMaquina, 10);
        Assert.Equal(BaseEsperada, resultado.CostoBase, 10);
        Assert.Equal(MargenErrorEsperado, resultado.MargenErrorValor, 10);
        Assert.Equal(IvaLuzEsperado, resultado.IvaLuzValor, 10);
        Assert.Equal(CostoTotalEsperado, resultado.CostoTotal, 10);
        Assert.Equal(PrecioVentaEsperado, resultado.PrecioVenta, 10);
        Assert.Equal(PrecioVentaEsperado, resultado.PrecioFinalConEnvio, 10);
    }

    [Fact]
    public void Calcular_Conserva_ElContextoUsado()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());

        Assert.Equal(1.5d, resultado.MargenGanancia);
        Assert.Equal(21d, resultado.IvaLuzPct);
        Assert.Equal(10d, resultado.MargenErrorPct);
        Assert.Equal(0d, resultado.CostoEnvio);
    }

    [Fact]
    public void Calcular_MaterialEsGramosSobreMilPorPrecioKg()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Gramos = 37.5;
        entrada.Filamento!.PriceKg = 18500;

        var resultado = QuoteCalculator.Calcular(entrada);

        Assert.Equal(693.75d, resultado.PrecioMaterial, 10);
    }

    [Fact]
    public void Calcular_ElIvaSeAplicaSoloSobreLaLuz()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());

        Assert.Equal(resultado.PrecioLuz * 0.21d, resultado.IvaLuzValor, 12);
        Assert.NotEqual(resultado.CostoBase * 0.21d, resultado.IvaLuzValor, 6);
    }

    [Fact]
    public void Calcular_ElMargenDeErrorSeAplicaSoloSobreElCostoBase()
    {
        var resultado = QuoteCalculator.Calcular(TestData.EntradaPorDefecto());

        Assert.Equal(resultado.CostoBase * 0.10d, resultado.MargenErrorValor, 12);
    }

    [Fact]
    public void Calcular_TiempoDesdeDiasHorasMinutosYSegundos()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Dias = 1;
        entrada.Horas = 2;
        entrada.Minutos = 30;
        entrada.Segundos = 36;

        var resultado = QuoteCalculator.Calcular(entrada);

        // (1 * 24) + 2 + (30 / 60) + (36 / 3600) = 26,51
        Assert.Equal(26.51d, resultado.HorasImpresion, 10);
    }

    [Fact]
    public void Calcular_AceptaHorasFraccionarias()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Horas = 1.5;
        entrada.Minutos = 0;

        var resultado = QuoteCalculator.Calcular(entrada);

        Assert.Equal(1.5d, resultado.HorasImpresion, 10);
    }

    [Fact]
    public void CalcularHoras_UsaElMismoOrdenQueElLegado()
    {
        Assert.Equal(2.5d, QuoteCalculator.CalcularHoras(0, 2, 30, 0), 10);
        Assert.Equal(24d, QuoteCalculator.CalcularHoras(1, 0, 0, 0), 10);
        Assert.Equal(1d / 60d, QuoteCalculator.CalcularHoras(0, 0, 1, 0), 12);
        Assert.Equal(1d / 3600d, QuoteCalculator.CalcularHoras(0, 0, 0, 1), 12);
    }

    [Fact]
    public void Calcular_VidaUtilCero_DesgasteEsCero()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Settings.DesgasteHoras = 0;

        var resultado = QuoteCalculator.Calcular(entrada);

        Assert.Equal(0d, resultado.DesgasteMaquina);
        Assert.Equal(MaterialEsperado + LuzEsperada, resultado.CostoBase, 10);
        Assert.True(QuoteCalculator.DesgasteAnulado(entrada.Settings));
    }

    [Fact]
    public void DesgasteAnulado_EsFalsoConVidaUtilPositiva()
    {
        Assert.False(QuoteCalculator.DesgasteAnulado(TestData.SettingsPorDefecto()));
    }

    [Fact]
    public void Calcular_ElEnvioSeSumaDespuesDelMargenDeGanancia()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Settings.CostoEnvio = 3500;

        var resultado = QuoteCalculator.Calcular(entrada);

        Assert.Equal(PrecioVentaEsperado, resultado.PrecioVenta, 10);
        Assert.Equal(PrecioVentaEsperado + 3500d, resultado.PrecioFinalConEnvio, 10);

        // Si el envio entrara antes del margen, el total seria otro.
        Assert.NotEqual((CostoTotalEsperado + 3500d) * 1.5d, resultado.PrecioFinalConEnvio, 6);
    }

    [Fact]
    public void Calcular_MargenGananciaUno_NoAgregaGanancia()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Settings.MargenGanancia = 1;

        var resultado = QuoteCalculator.Calcular(entrada);

        Assert.Equal(resultado.CostoTotal, resultado.PrecioVenta, 12);
    }

    [Fact]
    public void Calcular_SinFilamento_Lanza()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Filamento = null;

        var ex = Assert.Throws<QuoteValidationException>(() => QuoteCalculator.Calcular(entrada));
        Assert.Equal("Filamento no válido o no seleccionado.", ex.Message);
        Assert.Equal("filamento", ex.Campo);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    public void Calcular_TiempoCero_Lanza(double dias, double horas, double minutos, double segundos)
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Dias = dias;
        entrada.Horas = horas;
        entrada.Minutos = minutos;
        entrada.Segundos = segundos;

        var ex = Assert.Throws<QuoteValidationException>(() => QuoteCalculator.Calcular(entrada));
        Assert.Contains("tiempo de impresión", ex.Message, StringComparison.Ordinal);
        Assert.Equal("tiempo", ex.Campo);
    }

    [Fact]
    public void Calcular_GramosCero_Lanza()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Gramos = 0;

        var ex = Assert.Throws<QuoteValidationException>(() => QuoteCalculator.Calcular(entrada));
        Assert.Contains("gramos", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("gramos", ex.Campo);
    }

    [Fact]
    public void Calcular_GramosNegativos_Lanza()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Gramos = -1;

        var ex = Assert.Throws<QuoteValidationException>(() => QuoteCalculator.Calcular(entrada));
        Assert.Equal("gramos", ex.Campo);
    }

    [Theory]
    [InlineData("precio_kwh")]
    [InlineData("consumo_w")]
    [InlineData("desgaste_horas")]
    [InlineData("precio_repuestos")]
    [InlineData("margen_error_pct")]
    [InlineData("iva_luz_pct")]
    [InlineData("margen_ganancia_x")]
    [InlineData("costo_envio")]
    public void Calcular_SettingNegativo_Lanza(string campo)
    {
        var entrada = TestData.EntradaPorDefecto();
        switch (campo)
        {
            case "precio_kwh": entrada.Settings.PrecioKwh = -1; break;
            case "consumo_w": entrada.Settings.ConsumoW = -1; break;
            case "desgaste_horas": entrada.Settings.DesgasteHoras = -1; break;
            case "precio_repuestos": entrada.Settings.PrecioRepuestos = -1; break;
            case "margen_error_pct": entrada.Settings.MargenErrorPct = -1; break;
            case "iva_luz_pct": entrada.Settings.IvaLuzPct = -1; break;
            case "margen_ganancia_x": entrada.Settings.MargenGanancia = -1; break;
            case "costo_envio": entrada.Settings.CostoEnvio = -1; break;
            default: throw new InvalidOperationException(campo);
        }

        var ex = Assert.Throws<QuoteValidationException>(() => QuoteCalculator.Calcular(entrada));
        Assert.Equal(campo, ex.Campo);
        Assert.Contains("no puede ser neg", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Calcular_TiempoNegativo_Lanza()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Dias = -1;
        entrada.Horas = 25;

        var ex = Assert.Throws<QuoteValidationException>(() => QuoteCalculator.Calcular(entrada));
        Assert.Equal("dias", ex.Campo);
    }

    [Fact]
    public void Calcular_PrecioDeFilamentoNegativo_Lanza()
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Filamento!.PriceKg = -10;

        var ex = Assert.Throws<QuoteValidationException>(() => QuoteCalculator.Calcular(entrada));
        Assert.Equal("price_kg", ex.Campo);
    }

    [Fact]
    public void Calcular_EntradaNula_Lanza()
    {
        Assert.Throws<ArgumentNullException>(() => QuoteCalculator.Calcular(null!));
    }
}

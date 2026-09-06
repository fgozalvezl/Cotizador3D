using Cotizador3D.Core.Formatting;
using Xunit;

namespace Cotizador3D.Core.Tests;

public class MoneyFormatTests
{
    [Theory]
    [InlineData(0d, "$ 0,00")]
    [InlineData(1234.5d, "$ 1.234,50")]
    [InlineData(1234.56d, "$ 1.234,56")]
    [InlineData(4523.8131285d, "$ 4.523,81")]
    [InlineData(1234567.891d, "$ 1.234.567,89")]
    [InlineData(-1234.5d, "$ -1.234,50")]
    [InlineData(0.005d, "$ 0,01")]
    public void Moneda_FormatoEsAr(double valor, string esperado)
    {
        Assert.Equal(esperado, MoneyFormat.Moneda(valor));
    }

    [Fact]
    public void MonedaPorKg_SinEspacioYConSufijo()
    {
        Assert.Equal("$18.500,00/kg", MoneyFormat.MonedaPorKg(18500));
    }

    [Fact]
    public void Numero_PermiteOtraCantidadDeDecimales()
    {
        Assert.Equal("2.727,40", MoneyFormat.Numero(2727.4049));
        Assert.Equal("2.727,4049", MoneyFormat.Numero(2727.4049, 4));
        Assert.Equal("2.727", MoneyFormat.Numero(2727.4049, 0));
    }

    [Fact]
    public void Numero_CeroNegativoSeMuestraSinSigno()
    {
        Assert.Equal("$ 0,00", MoneyFormat.Moneda(-0d));
    }

    /// <summary>
    /// Un residuo negativo minusculo (tipico del redondeo del desglose) no
    /// puede imprimirse como "-0,00".
    /// </summary>
    [Theory]
    [InlineData(-0.0000001d)]
    [InlineData(-0.001d)]
    [InlineData(-0.004999d)]
    [InlineData(-1e-15d)]
    public void Numero_NegativoDespreciable_SeImprimeComoCero(double valor)
    {
        Assert.Equal("0,00", MoneyFormat.Numero(valor));
        Assert.Equal("$ 0,00", MoneyFormat.Moneda(valor));
        Assert.Equal("$0,00/kg", MoneyFormat.MonedaPorKg(valor));
    }

    [Fact]
    public void Numero_PositivoDespreciable_TambienSeImprimeComoCero()
    {
        Assert.Equal("$ 0,00", MoneyFormat.Moneda(0.004d));
        Assert.Equal("$ 0,01", MoneyFormat.Moneda(0.006d));
    }

    /// <summary>El umbral acompana a los decimales pedidos, no siempre 0,005.</summary>
    [Fact]
    public void Numero_ConOtrosDecimales_SoloRedondeaACeroLoQueCorresponde()
    {
        Assert.Equal("-0,0049", MoneyFormat.Numero(-0.0049d, 4));
        Assert.Equal("0,0000", MoneyFormat.Numero(-0.000049d, 4));
        Assert.Equal("0,0", MoneyFormat.Numero(-0.04d, 1));
        Assert.Equal("-0,1", MoneyFormat.Numero(-0.06d, 1));
    }

    [Theory]
    [InlineData(21d, "21")]
    [InlineData(10.5d, "10,5")]
    [InlineData(0d, "0")]
    [InlineData(10.25d, "10,25")]
    public void Porcentaje_SinCerosSobrantes(double valor, string esperado)
    {
        Assert.Equal(esperado, MoneyFormat.Porcentaje(valor));
    }

    [Fact]
    public void Horas_DosDecimalesEnEsAr()
    {
        Assert.Equal("2,50 h", MoneyFormat.Horas(2.5));
        Assert.Equal("26,51 h", MoneyFormat.Horas(26.51));
    }

    [Theory]
    [InlineData(2.5d, "2 h 30 min")]
    [InlineData(26.51d, "1 d 2 h 30 min 36 s")]
    [InlineData(0.5d, "30 min")]
    [InlineData(24d, "1 d")]
    [InlineData(0d, "0 min")]
    [InlineData(1d / 3600d, "1 s")]
    public void Duracion_LegibleEnEspanol(double horas, string esperado)
    {
        Assert.Equal(esperado, MoneyFormat.Duracion(horas));
    }
}

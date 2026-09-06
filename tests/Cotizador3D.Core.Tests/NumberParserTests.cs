using Cotizador3D.Core.Calculation;
using Xunit;

namespace Cotizador3D.Core.Tests;

public class NumberParserTests
{
    [Theory]
    [InlineData("1234.56", 1234.56)]
    [InlineData("1234,56", 1234.56)]
    [InlineData("  25  ", 25)]
    [InlineData("-3,5", -3.5)]
    [InlineData("0", 0)]
    [InlineData("199.74640", 199.7464)]
    public void Parse_AceptaComaOPunto(string texto, double esperado)
    {
        Assert.Equal(esperado, NumberParser.Parse(texto), 10);
    }

    [Fact]
    public void Parse_TextoVacio_UsaElValorPorDefecto()
    {
        Assert.Equal(0d, NumberParser.Parse(""));
        Assert.Equal(1.5d, NumberParser.Parse("", 1.5));
        Assert.Equal(1.5d, NumberParser.Parse(null, 1.5));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.234,56")]
    [InlineData("12,5,5")]
    public void Parse_TextoInvalido_Lanza(string texto)
    {
        var ex = Assert.Throws<QuoteValidationException>(() => NumberParser.Parse(texto, 0, "gramos"));
        Assert.Equal("gramos", ex.Campo);
        Assert.Contains("no es un número válido", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TryParse_DevuelveFalseSinLanzar()
    {
        Assert.False(NumberParser.TryParse("xx", out var valor, 7));
        Assert.Equal(7d, valor);
    }

    [Fact]
    public void ParseOrDefault_CaeAlDefaultSiEsInvalido()
    {
        Assert.Equal(21d, NumberParser.ParseOrDefault("no es un numero", 21));
        Assert.Equal(150d, NumberParser.ParseOrDefault("150", 0));
    }

    [Theory]
    [InlineData(199.7464, "199.7464")]
    [InlineData(150d, "150")]
    [InlineData(0d, "0")]
    [InlineData(1.5d, "1.5")]
    [InlineData(-2.25d, "-2.25")]
    public void ToInvariantString_UsaPuntoDecimalYSinSeparadorDeMiles(double valor, string esperado)
    {
        Assert.Equal(esperado, NumberParser.ToInvariantString(valor));
    }

    [Fact]
    public void ToInvariantString_MilesSinSeparador()
    {
        Assert.Equal("305000", NumberParser.ToInvariantString(305000d));
    }
}

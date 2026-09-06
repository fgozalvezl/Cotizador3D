using System.Text;
using Cotizador3D.Core.Calculation;
using Cotizador3D.Core.Export;
using Cotizador3D.Core.Formatting;
using Cotizador3D.Core.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Xunit;

namespace Cotizador3D.Core.Tests;

/// <summary>
/// Verifica el PDF que ve el cliente: que se genere, que muestre el desglose
/// escalado y, sobre todo, que NO revele la ganancia (docs/DECISIONS.md).
/// El texto se extrae del PDF real con PdfPig.
/// </summary>
public class QuotePdfExporterTests
{
    // ---------------------------------------------------------------- datos

    /// <summary>Trabajo de referencia con el envio indicado.</summary>
    private static (QuoteResult Resultado, ClientQuoteInfo Info) Referencia(
        double costoEnvio = 0,
        double margenGanancia = 1.5,
        string? nombreNegocio = "Impresiones Patagonia",
        string? cliente = "Juan Pérez",
        string? trabajo = "Soporte de monitor")
    {
        var entrada = TestData.EntradaPorDefecto();
        entrada.Settings.CostoEnvio = costoEnvio;
        entrada.Settings.MargenGanancia = margenGanancia;

        var resultado = QuoteCalculator.Calcular(entrada);

        var info = new ClientQuoteInfo
        {
            NombreNegocio = nombreNegocio,
            Cliente = cliente,
            Trabajo = trabajo,
            Filamento = entrada.Filamento!,
            Gramos = entrada.Gramos,
            HorasImpresion = resultado.HorasImpresion,
            Fecha = new DateTime(2026, 9, 6),
        };

        return (resultado, info);
    }

    /// <summary>Texto del PDF, tal como lo lee un extractor.</summary>
    private static string TextoDelPdf(byte[] pdf)
    {
        using var documento = PdfDocument.Open(pdf);
        var texto = new StringBuilder();

        foreach (var pagina in documento.GetPages())
        {
            texto.AppendLine(ContentOrderTextExtractor.GetText(pagina));
        }

        return texto.ToString();
    }

    /// <summary>
    /// Texto sin ningun espacio: hace las comparaciones inmunes a como el
    /// extractor reparta los espacios entre glifos ("$ 1.234,56" / "$1.234,56").
    /// </summary>
    private static string SinEspacios(string texto) =>
        new(texto.Where(c => !char.IsWhiteSpace(c)).ToArray());

    private static void Contiene(string textoPlano, string esperado) =>
        Assert.Contains(SinEspacios(esperado), textoPlano, StringComparison.OrdinalIgnoreCase);

    private static void NoContiene(string textoPlano, string prohibido) =>
        Assert.DoesNotContain(SinEspacios(prohibido), textoPlano, StringComparison.OrdinalIgnoreCase);

    // ------------------------------------------------------- PDF bien formado

    [Fact]
    public void Generar_DevuelveUnPdfValido()
    {
        var (resultado, info) = Referencia();

        var pdf = QuotePdfExporter.Generar(resultado, info);

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF"u8.ToArray(), pdf.Take(4).ToArray());
    }

    [Fact]
    public void Generar_ProduceUnaSolaPagina()
    {
        var (resultado, info) = Referencia(costoEnvio: 3500);

        using var documento = PdfDocument.Open(QuotePdfExporter.Generar(resultado, info));

        Assert.Equal(1, documento.NumberOfPages);
    }

    // ------------------------------------------------------------- contenido

    [Fact]
    public void Generar_IncluyeCadaConceptoYCadaImporteDelDesglose()
    {
        var (resultado, info) = Referencia(costoEnvio: 3500);
        var desglose = resultado.ParaCliente();

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        Assert.Equal(6, desglose.Lineas.Count);
        foreach (var linea in desglose.Lineas)
        {
            Contiene(texto, linea.Concepto);
            Contiene(texto, MoneyFormat.Moneda(linea.Importe));
        }
    }

    [Fact]
    public void Generar_IncluyeElTotalFormateado()
    {
        var (resultado, info) = Referencia(costoEnvio: 3500);
        var desglose = resultado.ParaCliente();

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        Contiene(texto, QuotePdfExporter.RotuloTotal);
        Contiene(texto, MoneyFormat.Moneda(desglose.Total));
    }

    [Fact]
    public void Generar_IncluyeFilamentoGramosYTiempo()
    {
        var (resultado, info) = Referencia();

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        Contiene(texto, "Grilon3 (PLA)");
        Contiene(texto, "100,0 g");
        Contiene(texto, "2 h 30 min");
    }

    [Fact]
    public void Generar_IncluyeLaFechaYLaNotaAlPie()
    {
        var (resultado, info) = Referencia();

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        Contiene(texto, "06/09/2026");
        Contiene(texto, QuotePdfExporter.NotaAlPie);
    }

    // ------------------------------------------------ regla: nada de ganancia

    [Fact]
    public void Generar_NoRevelaLaGananciaNiElCostoReal()
    {
        // Con margen 1,5 y envio > 0, el costo total y el precio de venta son
        // distintos del total que ve el cliente: si aparecieran, se notaria.
        var (resultado, info) = Referencia(costoEnvio: 3500, margenGanancia: 1.5);
        var desglose = resultado.ParaCliente();

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        NoContiene(texto, "ganancia");
        NoContiene(texto, "margen de ganancia");
        NoContiene(texto, "venta");
        NoContiene(texto, "precio de venta");
        NoContiene(texto, "costo total");

        Assert.NotEqual(resultado.CostoTotal, desglose.Total, 6);
        Assert.NotEqual(resultado.PrecioVenta, desglose.Total, 6);
        NoContiene(texto, MoneyFormat.Moneda(resultado.CostoTotal));
        NoContiene(texto, MoneyFormat.Moneda(resultado.PrecioVenta));
        NoContiene(texto, MoneyFormat.Moneda(resultado.CostoBase));
    }

    [Theory]
    [InlineData(1.5d)]
    [InlineData(2.4d)]
    [InlineData(3d)]
    public void Generar_ConDistintosMargenes_NuncaMuestraElMultiplicador(double margen)
    {
        var (resultado, info) = Referencia(costoEnvio: 1200, margenGanancia: margen);

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        NoContiene(texto, "ganancia");
        NoContiene(texto, "venta");
        NoContiene(texto, MoneyFormat.Moneda(resultado.PrecioVenta));
        NoContiene(texto, MoneyFormat.Numero(resultado.MargenGanancia));
        Assert.DoesNotContain(MoneyFormat.Porcentaje(margen) + "x", texto, StringComparison.OrdinalIgnoreCase);
    }

    // ----------------------------------------------------------------- envio

    [Fact]
    public void Generar_SinEnvio_NoMuestraLaLineaDeEnvio()
    {
        var (resultado, info) = Referencia(costoEnvio: 0);

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        NoContiene(texto, ClientQuoteBreakdown.ConceptoEnvio);
    }

    [Fact]
    public void Generar_ConEnvio_MuestraLaLineaDeEnvio()
    {
        var (resultado, info) = Referencia(costoEnvio: 3500);

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        Contiene(texto, ClientQuoteBreakdown.ConceptoEnvio);
        Contiene(texto, MoneyFormat.Moneda(3500));
    }

    // ------------------------------------------------------ datos opcionales

    [Fact]
    public void Generar_SinDatosOpcionales_UsaElTituloGenericoYOmiteLasFilas()
    {
        var (resultado, info) = Referencia(nombreNegocio: null, cliente: null, trabajo: null);

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        Contiene(texto, QuotePdfExporter.TituloGenerico);
        NoContiene(texto, "Cliente");
        NoContiene(texto, "Trabajo");

        // El resto del documento sigue completo.
        Contiene(texto, "Grilon3 (PLA)");
        Contiene(texto, QuotePdfExporter.RotuloTotal);
    }

    [Fact]
    public void Generar_ConEspaciosEnBlanco_LosTrataComoAusentes()
    {
        var (resultado, info) = Referencia(nombreNegocio: "   ", cliente: "", trabajo: "\t");

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        Contiene(texto, QuotePdfExporter.TituloGenerico);
        NoContiene(texto, "Cliente");
        NoContiene(texto, "Trabajo");
    }

    [Fact]
    public void Generar_ConDatosOpcionales_LosMuestra()
    {
        var (resultado, info) = Referencia(
            nombreNegocio: "Impresiones Patagonia",
            cliente: "Juan Pérez",
            trabajo: "Soporte de monitor");

        var texto = SinEspacios(TextoDelPdf(QuotePdfExporter.Generar(resultado, info)));

        Contiene(texto, "Impresiones Patagonia");
        Contiene(texto, "Cliente");
        Contiene(texto, "Juan Pérez");
        Contiene(texto, "Trabajo");
        Contiene(texto, "Soporte de monitor");

        // Con nombre de negocio, el titulo generico pasa a subtitulo.
        Contiene(texto, QuotePdfExporter.TituloGenerico);
    }

    // -------------------------------------------------------------- Exportar

    [Fact]
    public void Exportar_EscribeElArchivoEnLaRutaIndicada()
    {
        using var temporal = new DirectorioTemporal();
        var ruta = temporal.Archivo("cotizacion.pdf");
        var (resultado, info) = Referencia(costoEnvio: 3500);

        QuotePdfExporter.Exportar(resultado, info, ruta);

        Assert.True(File.Exists(ruta));
        var bytes = File.ReadAllBytes(ruta);
        Assert.Equal("%PDF"u8.ToArray(), bytes.Take(4).ToArray());
        Contiene(SinEspacios(TextoDelPdf(bytes)), MoneyFormat.Moneda(resultado.ParaCliente().Total));
    }

    [Fact]
    public void Exportar_CreaElDirectorioSiNoExiste()
    {
        using var temporal = new DirectorioTemporal();
        var ruta = Path.Combine(temporal.Ruta, "sub", "carpeta", "cotizacion.pdf");
        var (resultado, info) = Referencia();

        QuotePdfExporter.Exportar(resultado, info, ruta);

        Assert.True(File.Exists(ruta));
    }

    [Fact]
    public void Exportar_RutaVacia_Lanza()
    {
        var (resultado, info) = Referencia();

        Assert.Throws<ArgumentException>(() => QuotePdfExporter.Exportar(resultado, info, "   "));
    }

    [Fact]
    public void Generar_ArgumentosNulos_Lanza()
    {
        var (resultado, info) = Referencia();

        Assert.Throws<ArgumentNullException>(() => QuotePdfExporter.Generar(null!, info));
        Assert.Throws<ArgumentNullException>(() => QuotePdfExporter.Generar(resultado, null!));
    }
}

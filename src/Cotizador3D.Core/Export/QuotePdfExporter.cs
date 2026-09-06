using System.Globalization;
using Cotizador3D.Core.Formatting;
using Cotizador3D.Core.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cotizador3D.Core.Export;

/// <summary>
/// Genera el PDF que se le entrega al cliente.
///
/// Regla de negocio (docs/DECISIONS.md, seccion "PDF para el cliente"): el
/// documento NUNCA menciona la ganancia ni el precio de venta ni el costo real.
/// Solo se imprimen las lineas ya escaladas de
/// <see cref="QuoteResult.ParaCliente"/>, el envio (si es mayor a cero) y el
/// total. Por eso el exportador jamas lee <c>MargenGanancia</c>,
/// <c>CostoTotal</c> ni <c>PrecioVenta</c> del resultado.
/// </summary>
public static class QuotePdfExporter
{
    /// <summary>Titulo usado cuando no hay nombre de negocio configurado.</summary>
    public const string TituloGenerico = "Cotización de impresión 3D";

    /// <summary>Rotulo de la fila del total.</summary>
    public const string RotuloTotal = "TOTAL";

    /// <summary>Nota fija al pie del documento.</summary>
    public const string NotaAlPie = "Precios expresados en pesos. Cotización válida por 7 días.";

    // Paleta sobria; los colores se declaran como hexadecimal para no depender
    // del tema del sistema.
    private const string ColorTexto = "#1F2933";
    private const string ColorTextoSuave = "#6B7280";
    private const string ColorLinea = "#D7DCE2";
    private const string ColorFondoTabla = "#F3F5F7";
    private const string ColorAcento = "#2F5D8C";

    private static readonly object CandadoLicencia = new();
    private static bool _licenciaConfigurada;

    /// <summary>
    /// Genera el PDF en memoria. Util para previsualizar o para enviarlo sin
    /// tocar el disco.
    /// </summary>
    public static byte[] Generar(QuoteResult resultado, ClientQuoteInfo info)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        ArgumentNullException.ThrowIfNull(info);

        ConfigurarLicencia();

        var desglose = resultado.ParaCliente();
        return Construir(desglose, info).GeneratePdf();
    }

    /// <summary>
    /// Genera el PDF y lo escribe en <paramref name="rutaSalida"/>, creando el
    /// directorio destino si hace falta.
    /// </summary>
    public static void Exportar(QuoteResult resultado, ClientQuoteInfo info, string rutaSalida)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        ArgumentNullException.ThrowIfNull(info);
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaSalida);

        var bytes = Generar(resultado, info);

        var directorio = Path.GetDirectoryName(Path.GetFullPath(rutaSalida));
        if (!string.IsNullOrEmpty(directorio))
        {
            Directory.CreateDirectory(directorio);
        }

        File.WriteAllBytes(rutaSalida, bytes);
    }

    /// <summary>
    /// Activa la licencia Community de QuestPDF. Es idempotente y se ejecuta
    /// desde el propio exportador para que ningun llamador pueda olvidarla.
    /// </summary>
    private static void ConfigurarLicencia()
    {
        if (_licenciaConfigurada)
        {
            return;
        }

        lock (CandadoLicencia)
        {
            if (_licenciaConfigurada)
            {
                return;
            }

            QuestPDF.Settings.License = LicenseType.Community;
            _licenciaConfigurada = true;
        }
    }

    private static IDocument Construir(ClientQuoteBreakdown desglose, ClientQuoteInfo info) =>
        Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.PageColor(Colors.White);

                // Lato viene embebida en QuestPDF: el PDF se ve igual en
                // cualquier maquina, sin instalar fuentes.
                // Lato tiene una ligadura "ti" cuya tabla ToUnicode rompe el
                // copiado y la busqueda de texto ("Cotizacion" se copia mal).
                // Se desactivan las ligaduras estandar para que el PDF sea
                // literal al seleccionarlo.
                pagina.DefaultTextStyle(t => t
                    .FontFamily(Fonts.Lato)
                    .FontSize(10)
                    .FontColor(ColorTexto)
                    .DisableFontFeature(FontFeatures.StandardLigatures));

                pagina.Header().Element(c => Encabezado(c, info));
                pagina.Content().Element(c => Cuerpo(c, desglose, info));
                pagina.Footer().Element(Pie);
            });
        });

    private static void Encabezado(IContainer contenedor, ClientQuoteInfo info)
    {
        var negocio = Limpiar(info.NombreNegocio);
        var titulo = negocio ?? TituloGenerico;

        contenedor.Column(columna =>
        {
            columna.Item().Row(fila =>
            {
                fila.RelativeItem().Column(izquierda =>
                {
                    izquierda.Item().Text(titulo).FontSize(20).SemiBold().FontColor(ColorAcento);

                    if (negocio is not null)
                    {
                        izquierda.Item().PaddingTop(2).Text(TituloGenerico)
                            .FontSize(10).FontColor(ColorTextoSuave);
                    }
                });

                fila.ConstantItem(150).AlignRight().Column(derecha =>
                {
                    derecha.Item().Text("Fecha").FontSize(8).FontColor(ColorTextoSuave);
                    derecha.Item().Text(FormatearFecha(info.Fecha)).FontSize(11).SemiBold();
                });
            });

            columna.Item().PaddingTop(10).LineHorizontal(1).LineColor(ColorAcento);
        });
    }

    private static void Cuerpo(IContainer contenedor, ClientQuoteBreakdown desglose, ClientQuoteInfo info)
    {
        contenedor.PaddingTop(18).Column(columna =>
        {
            columna.Item().Element(c => Detalles(c, info));
            columna.Item().PaddingTop(22).Element(c => Tabla(c, desglose));
        });
    }

    private static void Detalles(IContainer contenedor, ClientQuoteInfo info)
    {
        var filas = new List<(string Rotulo, string Valor)>(5);

        var cliente = Limpiar(info.Cliente);
        if (cliente is not null)
        {
            filas.Add(("Cliente", cliente));
        }

        var trabajo = Limpiar(info.Trabajo);
        if (trabajo is not null)
        {
            filas.Add(("Trabajo", trabajo));
        }

        filas.Add(("Filamento", info.Filamento.DisplayName));
        filas.Add(("Material", MoneyFormat.Numero(info.Gramos, 1) + " g"));
        filas.Add(("Tiempo de impresión", MoneyFormat.Duracion(info.HorasImpresion)));

        contenedor.Column(columna =>
        {
            foreach (var (rotulo, valor) in filas)
            {
                columna.Item().PaddingVertical(3).Row(fila =>
                {
                    fila.ConstantItem(130).Text(rotulo).FontColor(ColorTextoSuave);
                    fila.RelativeItem().Text(valor).SemiBold();
                });
            }
        });
    }

    private static void Tabla(IContainer contenedor, ClientQuoteBreakdown desglose)
    {
        contenedor.Column(columna =>
        {
            columna.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(columnas =>
                {
                    columnas.RelativeColumn(3);
                    columnas.RelativeColumn(1);
                });

                tabla.Header(encabezado =>
                {
                    encabezado.Cell().Element(CeldaEncabezado).Text("Detalle").SemiBold();
                    encabezado.Cell().Element(CeldaEncabezado).AlignRight().Text("Importe").SemiBold();
                });

                foreach (var linea in desglose.Lineas)
                {
                    tabla.Cell().Element(Celda).Text(linea.Concepto);
                    tabla.Cell().Element(Celda).AlignRight().Text(MoneyFormat.Moneda(linea.Importe));
                }
            });

            columna.Item().PaddingTop(6).BorderTop(1.5f).BorderColor(ColorAcento)
                .PaddingTop(8).PaddingHorizontal(8).Row(fila =>
            {
                fila.RelativeItem(3).Text(RotuloTotal).FontSize(13).Bold();
                fila.RelativeItem(1).AlignRight()
                    .Text(MoneyFormat.Moneda(desglose.Total)).FontSize(13).Bold().FontColor(ColorAcento);
            });
        });

        static IContainer CeldaEncabezado(IContainer celda) => celda
            .Background(ColorFondoTabla)
            .BorderBottom(1)
            .BorderColor(ColorLinea)
            .PaddingVertical(6)
            .PaddingHorizontal(8);

        static IContainer Celda(IContainer celda) => celda
            .BorderBottom(1)
            .BorderColor(ColorLinea)
            .PaddingVertical(6)
            .PaddingHorizontal(8);
    }

    private static void Pie(IContainer contenedor)
    {
        contenedor.Column(columna =>
        {
            columna.Item().PaddingBottom(6).LineHorizontal(1).LineColor(ColorLinea);
            columna.Item().AlignCenter().Text(NotaAlPie).FontSize(8).FontColor(ColorTextoSuave);
        });
    }

    /// <summary>Fecha en formato es-AR fijo (dd/MM/yyyy), sin depender de la cultura del sistema.</summary>
    private static string FormatearFecha(DateTime fecha) =>
        fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>Devuelve el texto sin espacios sobrantes, o null si esta vacio.</summary>
    private static string? Limpiar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}

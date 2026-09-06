using Cotizador3D.Core.Export;
using Cotizador3D.Core.Models;

namespace Cotizador3D.App.Services;

/// <summary>
/// Unico punto de la App que conoce el exportador del Core
/// (<c>QuotePdfExporter.Exportar(QuoteResult, ClientQuoteInfo, string)</c>).
/// Si el contrato del Core cambia, solo hay que tocar este archivo.
/// </summary>
public sealed class PdfExportService : IPdfExportService
{
    public void Exportar(QuoteResult resultado, DatosCotizacionCliente datos, string rutaSalida)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        ArgumentNullException.ThrowIfNull(datos);
        ArgumentException.ThrowIfNullOrWhiteSpace(rutaSalida);

        var info = new ClientQuoteInfo
        {
            NombreNegocio = string.IsNullOrWhiteSpace(datos.NombreNegocio) ? null : datos.NombreNegocio.Trim(),
            Cliente = string.IsNullOrWhiteSpace(datos.Cliente) ? null : datos.Cliente.Trim(),
            Trabajo = string.IsNullOrWhiteSpace(datos.Trabajo) ? null : datos.Trabajo.Trim(),
            Filamento = datos.Filamento,
            Gramos = datos.Gramos,
            HorasImpresion = datos.HorasImpresion,
            Fecha = datos.Fecha,
        };

        QuotePdfExporter.Exportar(resultado, info, rutaSalida);
    }
}

using Cotizador3D.Core.Models;

namespace Cotizador3D.App.Services;

/// <summary>
/// Datos de la cotizacion que van al PDF del cliente. Es el equivalente en la
/// App del contrato <c>Cotizador3D.Core.Models.ClientQuoteInfo</c>; la
/// traduccion vive en <see cref="PdfExportService"/> para que el resto de la
/// App no dependa del exportador.
/// </summary>
/// <param name="NombreNegocio">Nombre del negocio (opcional, de la configuracion).</param>
/// <param name="Cliente">Nombre del cliente (opcional, se pide al exportar).</param>
/// <param name="Trabajo">Nombre del trabajo (opcional, se pide al exportar).</param>
/// <param name="Filamento">Filamento usado en el calculo.</param>
/// <param name="Gramos">Gramos usados en el calculo.</param>
/// <param name="HorasImpresion">Horas de impresion del calculo.</param>
/// <param name="Fecha">Fecha de la cotizacion.</param>
public sealed record DatosCotizacionCliente(
    string? NombreNegocio,
    string? Cliente,
    string? Trabajo,
    Filament Filamento,
    double Gramos,
    double HorasImpresion,
    DateTime Fecha);

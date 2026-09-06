using Cotizador3D.Core.Models;

namespace Cotizador3D.App.Services;

/// <summary>Genera el PDF de la cotizacion para el cliente.</summary>
public interface IPdfExportService
{
    /// <exception cref="Exception">Cualquier fallo se propaga para mostrarlo al usuario.</exception>
    void Exportar(QuoteResult resultado, DatosCotizacionCliente datos, string rutaSalida);
}

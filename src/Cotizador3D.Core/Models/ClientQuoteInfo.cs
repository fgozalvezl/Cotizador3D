namespace Cotizador3D.Core.Models;

/// <summary>
/// Datos de presentacion de la cotizacion que ve el cliente
/// (docs/DECISIONS.md, seccion "PDF para el cliente"). No contiene ningun
/// valor economico interno: los importes salen de
/// <see cref="QuoteResult.ParaCliente"/>.
/// </summary>
public sealed class ClientQuoteInfo
{
    /// <summary>
    /// Nombre del negocio que emite la cotizacion. Opcional: si esta vacio, el
    /// PDF usa el titulo generico.
    /// </summary>
    public string? NombreNegocio { get; init; }

    /// <summary>Nombre del cliente. Opcional: si esta vacio no se muestra la fila.</summary>
    public string? Cliente { get; init; }

    /// <summary>Descripcion del trabajo. Opcional: si esta vacio no se muestra la fila.</summary>
    public string? Trabajo { get; init; }

    /// <summary>Filamento usado; se muestra como "Marca (Tipo)".</summary>
    public required Filament Filamento { get; init; }

    /// <summary>Gramos de material del trabajo.</summary>
    public double Gramos { get; init; }

    /// <summary>Tiempo de impresion en horas decimales.</summary>
    public double HorasImpresion { get; init; }

    /// <summary>Fecha de emision. Por defecto, el momento en que se crea el objeto.</summary>
    public DateTime Fecha { get; init; } = DateTime.Now;
}

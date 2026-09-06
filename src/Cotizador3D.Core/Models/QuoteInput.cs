namespace Cotizador3D.Core.Models;

/// <summary>
/// Entradas de una cotizacion: el filamento elegido, los gramos, el tiempo de
/// impresion descompuesto en dias/horas/minutos/segundos y una copia de la
/// configuracion vigente (docs/SPEC-legacy.md 2.1).
/// </summary>
public sealed class QuoteInput
{
    /// <summary>Filamento seleccionado. Null equivale a "no seleccionado".</summary>
    public Filament? Filamento { get; set; }

    /// <summary>Gramos de material a utilizar.</summary>
    public double Gramos { get; set; }

    public double Dias { get; set; }

    public double Horas { get; set; }

    public double Minutos { get; set; }

    public double Segundos { get; set; }

    /// <summary>Configuracion usada para este calculo (precios, porcentajes, envio).</summary>
    public AppSettings Settings { get; set; } = new();
}
